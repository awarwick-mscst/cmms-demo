using System.Drawing.Printing;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using CMMS.Core.Entities;
using CMMS.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace CMMS.Infrastructure.Services;

public class PrintService : IPrintService
{
    private readonly ZplGenerator _zplGenerator;
    private readonly EplGenerator _eplGenerator;
    private readonly ILogger<PrintService> _logger;
    private const int ConnectionTimeoutMs = 5000;
    private const int SendTimeoutMs = 10000;

    public PrintService(ILogger<PrintService> logger)
    {
        _zplGenerator = new ZplGenerator();
        _eplGenerator = new EplGenerator();
        _logger = logger;
    }

    public Task<string> GenerateLabelCommandsAsync(Part part, LabelTemplate template, PrinterLanguage language, CancellationToken cancellationToken = default)
    {
        var commands = language == PrinterLanguage.EPL
            ? _eplGenerator.GenerateLabel(part, template)
            : _zplGenerator.GenerateLabel(part, template);
        return Task.FromResult(commands);
    }

    public async Task<bool> PrintLabelAsync(Part part, LabelTemplate template, LabelPrinter printer, int quantity = 1, CancellationToken cancellationToken = default)
    {
        // Generate label commands based on printer language
        var singleLabel = printer.Language == PrinterLanguage.EPL
            ? _eplGenerator.GenerateLabel(part, template)
            : _zplGenerator.GenerateLabel(part, template);

        // Build commands for the specified quantity
        var sb = new StringBuilder();
        for (int i = 0; i < quantity; i++)
        {
            sb.Append(singleLabel);
        }

        var commands = sb.ToString();

        // Send to printer based on connection type
        return printer.ConnectionType == PrinterConnectionType.WindowsPrinter
            ? await SendToWindowsPrinterAsync(printer, commands, cancellationToken)
            : await SendToNetworkPrinterAsync(printer, commands, cancellationToken);
    }

    public async Task<bool> TestPrinterConnectionAsync(LabelPrinter printer, CancellationToken cancellationToken = default)
    {
        if (printer.ConnectionType == PrinterConnectionType.WindowsPrinter)
        {
            return await TestWindowsPrinterAsync(printer, cancellationToken);
        }

        return await TestNetworkPrinterAsync(printer, cancellationToken);
    }

    public Task<IEnumerable<string>> GetWindowsPrintersAsync(CancellationToken cancellationToken = default)
    {
        var printers = new List<string>();

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            foreach (string printerName in PrinterSettings.InstalledPrinters)
            {
                printers.Add(printerName);
            }
        }

        return Task.FromResult(printers.AsEnumerable());
    }

    private async Task<bool> TestNetworkPrinterAsync(LabelPrinter printer, CancellationToken cancellationToken)
    {
        try
        {
            using var client = new TcpClient();

            var connectTask = client.ConnectAsync(printer.IpAddress, printer.Port);
            var timeoutTask = Task.Delay(ConnectionTimeoutMs, cancellationToken);

            var completedTask = await Task.WhenAny(connectTask, timeoutTask);

            if (completedTask == timeoutTask)
            {
                _logger.LogWarning("Printer connection timeout: {PrinterName} at {IpAddress}:{Port}",
                    printer.Name, printer.IpAddress, printer.Port);
                return false;
            }

            await connectTask;

            if (!client.Connected)
            {
                return false;
            }

            _logger.LogInformation("Printer connection successful: {PrinterName} at {IpAddress}:{Port}",
                printer.Name, printer.IpAddress, printer.Port);

            return true;
        }
        catch (SocketException ex)
        {
            _logger.LogWarning(ex, "Printer connection failed: {PrinterName} at {IpAddress}:{Port}",
                printer.Name, printer.IpAddress, printer.Port);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error testing printer connection: {PrinterName}",
                printer.Name);
            return false;
        }
    }

    private Task<bool> TestWindowsPrinterAsync(LabelPrinter printer, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrEmpty(printer.WindowsPrinterName))
            {
                _logger.LogWarning("Windows printer name not configured for: {PrinterName}", printer.Name);
                return Task.FromResult(false);
            }

            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                _logger.LogWarning("Windows printing is only supported on Windows platform");
                return Task.FromResult(false);
            }

            // Check if printer exists in installed printers
            var printerExists = false;
            foreach (string installedPrinter in PrinterSettings.InstalledPrinters)
            {
                if (installedPrinter.Equals(printer.WindowsPrinterName, StringComparison.OrdinalIgnoreCase))
                {
                    printerExists = true;
                    break;
                }
            }

            if (printerExists)
            {
                _logger.LogInformation("Windows printer found: {PrinterName} -> {WindowsPrinterName}",
                    printer.Name, printer.WindowsPrinterName);
            }
            else
            {
                _logger.LogWarning("Windows printer not found: {WindowsPrinterName}", printer.WindowsPrinterName);
            }

            return Task.FromResult(printerExists);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing Windows printer: {PrinterName}", printer.Name);
            return Task.FromResult(false);
        }
    }

    private async Task<bool> SendToNetworkPrinterAsync(LabelPrinter printer, string commands, CancellationToken cancellationToken)
    {
        try
        {
            using var client = new TcpClient();
            client.SendTimeout = SendTimeoutMs;

            var connectTask = client.ConnectAsync(printer.IpAddress, printer.Port);
            var timeoutTask = Task.Delay(ConnectionTimeoutMs, cancellationToken);

            var completedTask = await Task.WhenAny(connectTask, timeoutTask);

            if (completedTask == timeoutTask)
            {
                _logger.LogError("Printer connection timeout during print: {PrinterName} at {IpAddress}:{Port}",
                    printer.Name, printer.IpAddress, printer.Port);
                return false;
            }

            await connectTask;

            if (!client.Connected)
            {
                _logger.LogError("Failed to connect to printer: {PrinterName} at {IpAddress}:{Port}",
                    printer.Name, printer.IpAddress, printer.Port);
                return false;
            }

            using var stream = client.GetStream();
            var data = Encoding.ASCII.GetBytes(commands);

            await stream.WriteAsync(data, cancellationToken);
            await stream.FlushAsync(cancellationToken);

            _logger.LogInformation("Print job sent successfully to network printer {PrinterName}: {ByteCount} bytes",
                printer.Name, data.Length);

            return true;
        }
        catch (SocketException ex)
        {
            _logger.LogError(ex, "Socket error sending print job to {PrinterName} at {IpAddress}:{Port}",
                printer.Name, printer.IpAddress, printer.Port);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending print job to {PrinterName}",
                printer.Name);
            return false;
        }
    }

    private Task<bool> SendToWindowsPrinterAsync(LabelPrinter printer, string commands, CancellationToken cancellationToken)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            _logger.LogError("Windows printing is only supported on Windows platform");
            return Task.FromResult(false);
        }

        if (string.IsNullOrEmpty(printer.WindowsPrinterName))
        {
            _logger.LogError("Windows printer name not configured for: {PrinterName}", printer.Name);
            return Task.FromResult(false);
        }

        try
        {
            _logger.LogInformation("Sending print job to Windows printer: {WindowsPrinterName}, Data length: {Length} bytes",
                printer.WindowsPrinterName, commands.Length);

            // Log first 200 chars of commands for debugging
            var preview = commands.Length > 200 ? commands.Substring(0, 200) + "..." : commands;
            _logger.LogDebug("EPL/ZPL commands preview: {Commands}", preview.Replace("\r", "\\r").Replace("\n", "\\n"));

            // Send raw data to Windows printer using RawPrinterHelper
            var (success, errorMessage) = RawPrinterHelper.SendStringToPrinter(printer.WindowsPrinterName, commands);

            if (success)
            {
                _logger.LogInformation("Print job sent successfully to Windows printer {PrinterName} -> {WindowsPrinterName}",
                    printer.Name, printer.WindowsPrinterName);
            }
            else
            {
                _logger.LogError("Failed to send print job to Windows printer {WindowsPrinterName}: {Error}",
                    printer.WindowsPrinterName, errorMessage);
            }

            return Task.FromResult(success);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending print job to Windows printer {PrinterName}",
                printer.Name);
            return Task.FromResult(false);
        }
    }

    public Task<bool> PrintTestLabelAsync(LabelPrinter printer, CancellationToken cancellationToken = default)
    {
        string testCommands;

        if (printer.Language == PrinterLanguage.EPL)
        {
            testCommands = _eplGenerator.GenerateTestLabel(printer.Dpi);
        }
        else
        {
            // Simple ZPL test label
            testCommands = $"^XA\n^PW{2 * printer.Dpi}\n^LL{1 * printer.Dpi}\n^FO20,20^A0N,30,30^FDCMMS Test Label^FS\n^FO20,60^A0N,20,20^FDPrinter OK^FS\n^FO20,100^BY2^BCN,50,Y,N,N^FDTEST123^FS\n^XZ\n";
        }

        _logger.LogInformation("Sending test label to printer {PrinterName}, Language: {Language}",
            printer.Name, printer.Language);

        return printer.ConnectionType == PrinterConnectionType.WindowsPrinter
            ? SendToWindowsPrinterAsync(printer, testCommands, cancellationToken)
            : SendToNetworkPrinterAsync(printer, testCommands, cancellationToken);
    }
}

/// <summary>
/// Helper class to send raw data directly to a Windows printer.
/// This bypasses the Windows print driver and sends raw EPL/ZPL commands directly to the printer.
/// </summary>
public static class RawPrinterHelper
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct DOCINFO
    {
        [MarshalAs(UnmanagedType.LPWStr)]
        public string pDocName;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string? pOutputFile;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string? pDataType;
    }

    [DllImport("winspool.drv", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool OpenPrinter(string pPrinterName, out IntPtr hPrinter, IntPtr pDefault);

    [DllImport("winspool.drv", SetLastError = true)]
    private static extern bool ClosePrinter(IntPtr hPrinter);

    [DllImport("winspool.drv", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool StartDocPrinter(IntPtr hPrinter, int level, ref DOCINFO pDocInfo);

    [DllImport("winspool.drv", SetLastError = true)]
    private static extern bool EndDocPrinter(IntPtr hPrinter);

    [DllImport("winspool.drv", SetLastError = true)]
    private static extern bool StartPagePrinter(IntPtr hPrinter);

    [DllImport("winspool.drv", SetLastError = true)]
    private static extern bool EndPagePrinter(IntPtr hPrinter);

    [DllImport("winspool.drv", SetLastError = true)]
    private static extern bool WritePrinter(IntPtr hPrinter, IntPtr pBytes, int dwCount, out int dwWritten);

    public static (bool Success, string? ErrorMessage) SendStringToPrinter(string printerName, string data)
    {
        var bytes = Encoding.ASCII.GetBytes(data);
        return SendBytesToPrinter(printerName, bytes);
    }

    public static (bool Success, string? ErrorMessage) SendBytesToPrinter(string printerName, byte[] bytes)
    {
        var hPrinter = IntPtr.Zero;
        var pBytes = IntPtr.Zero;
        var success = false;
        string? errorMessage = null;

        try
        {
            if (!OpenPrinter(printerName, out hPrinter, IntPtr.Zero))
            {
                var errorCode = Marshal.GetLastWin32Error();
                errorMessage = $"OpenPrinter failed for '{printerName}'. Error code: {errorCode}. " +
                    GetPrinterErrorDescription(errorCode);
                return (false, errorMessage);
            }

            var docInfo = new DOCINFO
            {
                pDocName = "CMMS Label",
                pOutputFile = null,
                pDataType = "RAW"
            };

            if (!StartDocPrinter(hPrinter, 1, ref docInfo))
            {
                var errorCode = Marshal.GetLastWin32Error();
                errorMessage = $"StartDocPrinter failed. Error code: {errorCode}";
                ClosePrinter(hPrinter);
                return (false, errorMessage);
            }

            if (!StartPagePrinter(hPrinter))
            {
                var errorCode = Marshal.GetLastWin32Error();
                errorMessage = $"StartPagePrinter failed. Error code: {errorCode}";
                EndDocPrinter(hPrinter);
                ClosePrinter(hPrinter);
                return (false, errorMessage);
            }

            pBytes = Marshal.AllocCoTaskMem(bytes.Length);
            Marshal.Copy(bytes, 0, pBytes, bytes.Length);

            success = WritePrinter(hPrinter, pBytes, bytes.Length, out var bytesWritten);
            if (!success)
            {
                var errorCode = Marshal.GetLastWin32Error();
                errorMessage = $"WritePrinter failed. Error code: {errorCode}";
            }
            else if (bytesWritten != bytes.Length)
            {
                errorMessage = $"WritePrinter only wrote {bytesWritten} of {bytes.Length} bytes";
                success = false;
            }

            EndPagePrinter(hPrinter);
            EndDocPrinter(hPrinter);
        }
        catch (Exception ex)
        {
            errorMessage = $"Exception: {ex.Message}";
            success = false;
        }
        finally
        {
            if (pBytes != IntPtr.Zero)
            {
                Marshal.FreeCoTaskMem(pBytes);
            }

            if (hPrinter != IntPtr.Zero)
            {
                ClosePrinter(hPrinter);
            }
        }

        return (success, errorMessage);
    }

    private static string GetPrinterErrorDescription(int errorCode)
    {
        return errorCode switch
        {
            0 => "Success",
            2 => "Printer not found. Check the printer name matches exactly.",
            5 => "Access denied. Check printer permissions.",
            1722 => "RPC server unavailable. Printer may be offline.",
            1801 => "Invalid printer name.",
            _ => $"Unknown error ({errorCode})"
        };
    }
}
