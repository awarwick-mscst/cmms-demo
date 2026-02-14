using System.Diagnostics;
using System.Security.Cryptography;
using CMMS.Core.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CMMS.Infrastructure.Services;

public class UpdateService : IUpdateService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IHostApplicationLifetime _appLifetime;
    private readonly ILogger<UpdateService> _logger;
    private UpdateInfo? _availableUpdate;
    private readonly object _lock = new();

    public UpdateService(
        IHttpClientFactory httpClientFactory,
        IHostApplicationLifetime appLifetime,
        ILogger<UpdateService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _appLifetime = appLifetime;
        _logger = logger;
    }

    public UpdateInfo? GetAvailableUpdate()
    {
        lock (_lock) return _availableUpdate;
    }

    public void SetAvailableUpdate(UpdateInfo? update)
    {
        lock (_lock) _availableUpdate = update;
    }

    public async Task DownloadUpdateAsync(UpdateInfo update, CancellationToken ct = default)
    {
        var installDir = AppContext.BaseDirectory;
        var updatesDir = Path.Combine(installDir, "updates");
        Directory.CreateDirectory(updatesDir);

        var zipPath = Path.Combine(updatesDir, $"cmms-{update.Version}.zip");

        if (File.Exists(zipPath))
        {
            // Verify existing file hash
            if (await VerifyHashAsync(zipPath, update.Sha256Hash, ct))
            {
                _logger.LogInformation("Update zip already downloaded and verified: {Path}", zipPath);
                return;
            }
            File.Delete(zipPath);
        }

        _logger.LogInformation("Downloading update {Version} from {Url}", update.Version, update.DownloadUrl);

        var client = _httpClientFactory.CreateClient("LicenseServer");
        using var response = await client.GetAsync(update.DownloadUrl, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        var tempPath = zipPath + ".tmp";
        try
        {
            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            await using var file = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None);
            await stream.CopyToAsync(file, ct);
        }
        catch
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
            throw;
        }

        // Verify hash
        if (!string.IsNullOrEmpty(update.Sha256Hash) && !await VerifyHashAsync(tempPath, update.Sha256Hash, ct))
        {
            File.Delete(tempPath);
            throw new InvalidOperationException("Downloaded update file failed SHA256 verification.");
        }

        File.Move(tempPath, zipPath, overwrite: true);
        _logger.LogInformation("Update {Version} downloaded and verified: {Path}", update.Version, zipPath);
    }

    public void LaunchUpdater(string updateZipPath)
    {
        var installDir = AppContext.BaseDirectory;
        var updaterPath = Path.Combine(installDir, "CmmsUpdater.exe");

        if (!File.Exists(updaterPath))
            throw new FileNotFoundException("CmmsUpdater.exe not found in install directory.", updaterPath);

        if (!File.Exists(updateZipPath))
            throw new FileNotFoundException("Update zip not found.", updateZipPath);

        _logger.LogInformation("Launching updater: {Updater} --zip {Zip} --install-dir {Dir}",
            updaterPath, updateZipPath, installDir);

        var psi = new ProcessStartInfo
        {
            FileName = updaterPath,
            Arguments = $"--zip \"{updateZipPath}\" --install-dir \"{installDir}\" --service-name CmmsService",
            UseShellExecute = true,
            CreateNoWindow = false,
        };

        Process.Start(psi);

        // Stop the application so the updater can replace files
        _appLifetime.StopApplication();
    }

    private static async Task<bool> VerifyHashAsync(string filePath, string expectedHash, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(expectedHash)) return true;

        await using var stream = File.OpenRead(filePath);
        var hash = await SHA256.HashDataAsync(stream, ct);
        var actual = Convert.ToHexString(hash).ToLowerInvariant();
        return actual.Equals(expectedHash.ToLowerInvariant(), StringComparison.Ordinal);
    }
}
