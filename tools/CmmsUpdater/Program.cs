using System.Diagnostics;
using System.IO.Compression;
using System.ServiceProcess;

// CmmsUpdater: Stops the CMMS Windows Service, backs up current files,
// extracts the update zip, then restarts the service.
// Usage: CmmsUpdater.exe --zip <path> --install-dir <path> --service-name CmmsService

string? zipPath = null;
string? installDir = null;
string serviceName = "CmmsService";

for (int i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--zip" when i + 1 < args.Length:
            zipPath = args[++i];
            break;
        case "--install-dir" when i + 1 < args.Length:
            installDir = args[++i];
            break;
        case "--service-name" when i + 1 < args.Length:
            serviceName = args[++i];
            break;
    }
}

if (string.IsNullOrEmpty(zipPath) || string.IsNullOrEmpty(installDir))
{
    Console.Error.WriteLine("Usage: CmmsUpdater --zip <path> --install-dir <path> [--service-name CmmsService]");
    return 1;
}

if (!File.Exists(zipPath))
{
    Console.Error.WriteLine($"Update zip not found: {zipPath}");
    return 1;
}

if (!Directory.Exists(installDir))
{
    Console.Error.WriteLine($"Install directory not found: {installDir}");
    return 1;
}

var logPath = Path.Combine(installDir, "logs", "updater.log");
Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);

void Log(string message)
{
    var line = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] {message}";
    Console.WriteLine(line);
    try { File.AppendAllText(logPath, line + Environment.NewLine); } catch { }
}

var backupDir = Path.Combine(installDir, "backup-before-update");

// Directories/files to preserve during update (not overwritten)
var preservePaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
{
    "appsettings.Production.json",
    "wwwroot/uploads",
    "logs",
    "updates",
    "backup-before-update",
};

try
{
    // Step 1: Stop the Windows Service
    Log($"Stopping service '{serviceName}'...");
    if (!StopService(serviceName, TimeSpan.FromSeconds(60)))
    {
        Log("Warning: Could not stop service (may not be installed or already stopped).");
    }
    else
    {
        Log("Service stopped.");
    }

    // Wait a moment for file locks to release
    await Task.Delay(2000);

    // Step 2: Backup current files
    Log($"Creating backup at: {backupDir}");
    if (Directory.Exists(backupDir))
        Directory.Delete(backupDir, true);
    Directory.CreateDirectory(backupDir);

    foreach (var file in Directory.EnumerateFiles(installDir, "*", SearchOption.AllDirectories))
    {
        var relativePath = Path.GetRelativePath(installDir, file);

        // Skip backup directory itself and update zips
        if (relativePath.StartsWith("backup-before-update", StringComparison.OrdinalIgnoreCase) ||
            relativePath.StartsWith("updates", StringComparison.OrdinalIgnoreCase))
            continue;

        var destPath = Path.Combine(backupDir, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);
        File.Copy(file, destPath, true);
    }
    Log("Backup complete.");

    // Step 3: Extract update zip, preserving protected paths
    Log($"Extracting update from: {zipPath}");
    using (var archive = ZipFile.OpenRead(zipPath))
    {
        foreach (var entry in archive.Entries)
        {
            if (string.IsNullOrEmpty(entry.Name)) continue; // skip directory entries

            var entryPath = entry.FullName.Replace('/', Path.DirectorySeparatorChar);

            // Check if this path should be preserved
            bool shouldPreserve = false;
            foreach (var pp in preservePaths)
            {
                if (entryPath.StartsWith(pp, StringComparison.OrdinalIgnoreCase) ||
                    entryPath.Equals(pp, StringComparison.OrdinalIgnoreCase))
                {
                    // Only preserve if the file already exists
                    var existingPath = Path.Combine(installDir, entryPath);
                    if (File.Exists(existingPath) || Directory.Exists(existingPath))
                    {
                        shouldPreserve = true;
                        break;
                    }
                }
            }

            if (shouldPreserve)
            {
                Log($"  Preserving: {entryPath}");
                continue;
            }

            var destFile = Path.Combine(installDir, entryPath);
            Directory.CreateDirectory(Path.GetDirectoryName(destFile)!);
            entry.ExtractToFile(destFile, overwrite: true);
        }
    }
    Log("Extraction complete.");

    // Step 4: Start the service
    Log($"Starting service '{serviceName}'...");
    if (!StartService(serviceName, TimeSpan.FromSeconds(60)))
    {
        Log("ERROR: Failed to start service after update!");
        Log("Attempting rollback...");
        Rollback(backupDir, installDir, preservePaths);
        StartService(serviceName, TimeSpan.FromSeconds(60));
        return 2;
    }
    Log("Service started.");

    // Step 5: Health check
    Log("Performing health check...");
    await Task.Delay(5000); // Give the service time to initialize
    var healthy = await HealthCheckAsync("http://localhost:5000/api/v1/system/version");

    if (!healthy)
    {
        Log("ERROR: Health check failed! Rolling back...");
        StopService(serviceName, TimeSpan.FromSeconds(30));
        await Task.Delay(2000);
        Rollback(backupDir, installDir, preservePaths);
        StartService(serviceName, TimeSpan.FromSeconds(60));
        Log("Rollback complete, previous version restored.");
        return 2;
    }

    Log("Health check passed. Update successful!");

    // Cleanup: remove the update zip
    try { File.Delete(zipPath); } catch { }

    return 0;
}
catch (Exception ex)
{
    Log($"FATAL ERROR: {ex}");
    Log("Attempting rollback...");
    try
    {
        StopService(serviceName, TimeSpan.FromSeconds(30));
        await Task.Delay(2000);
        Rollback(backupDir, installDir, preservePaths);
        StartService(serviceName, TimeSpan.FromSeconds(60));
        Log("Rollback complete.");
    }
    catch (Exception rollbackEx)
    {
        Log($"Rollback also failed: {rollbackEx.Message}");
    }
    return 3;
}

static bool StopService(string name, TimeSpan timeout)
{
    try
    {
        using var sc = new ServiceController(name);
        if (sc.Status == ServiceControllerStatus.Stopped)
            return true;
        sc.Stop();
        sc.WaitForStatus(ServiceControllerStatus.Stopped, timeout);
        return true;
    }
    catch
    {
        return false;
    }
}

static bool StartService(string name, TimeSpan timeout)
{
    try
    {
        using var sc = new ServiceController(name);
        sc.Start();
        sc.WaitForStatus(ServiceControllerStatus.Running, timeout);
        return true;
    }
    catch
    {
        return false;
    }
}

static void Rollback(string backupDir, string installDir, HashSet<string> preservePaths)
{
    if (!Directory.Exists(backupDir)) return;

    foreach (var file in Directory.EnumerateFiles(backupDir, "*", SearchOption.AllDirectories))
    {
        var relativePath = Path.GetRelativePath(backupDir, file);

        bool shouldPreserve = false;
        foreach (var pp in preservePaths)
        {
            if (relativePath.StartsWith(pp, StringComparison.OrdinalIgnoreCase))
            {
                shouldPreserve = true;
                break;
            }
        }
        if (shouldPreserve) continue;

        var destPath = Path.Combine(installDir, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);
        File.Copy(file, destPath, true);
    }
}

static async Task<bool> HealthCheckAsync(string url)
{
    using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
    for (int attempt = 0; attempt < 5; attempt++)
    {
        try
        {
            var response = await client.GetAsync(url);
            if (response.IsSuccessStatusCode)
                return true;
        }
        catch
        {
            // Retry
        }
        await Task.Delay(3000);
    }
    return false;
}
