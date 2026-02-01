using System.Net.Sockets;
using System.Text;
using CMMS.Core.Entities;
using CMMS.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace CMMS.Infrastructure.Services;

public class PrintService : IPrintService
{
    private readonly ZplGenerator _zplGenerator;
    private readonly ILogger<PrintService> _logger;
    private const int ConnectionTimeoutMs = 5000;
    private const int SendTimeoutMs = 10000;

    public PrintService(ILogger<PrintService> logger)
    {
        _zplGenerator = new ZplGenerator();
        _logger = logger;
    }

    public Task<string> GenerateZplAsync(Part part, LabelTemplate template, CancellationToken cancellationToken = default)
    {
        var zpl = _zplGenerator.GenerateLabel(part, template);
        return Task.FromResult(zpl);
    }

    public async Task<bool> PrintLabelAsync(Part part, LabelTemplate template, LabelPrinter printer, int quantity = 1, CancellationToken cancellationToken = default)
    {
        var zpl = _zplGenerator.GenerateLabel(part, template);

        // Generate ZPL for the specified quantity
        var sb = new StringBuilder();
        for (int i = 0; i < quantity; i++)
        {
            sb.Append(zpl);
        }

        return await SendToPrinterAsync(printer, sb.ToString(), cancellationToken);
    }

    public async Task<bool> TestPrinterConnectionAsync(LabelPrinter printer, CancellationToken cancellationToken = default)
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

    private async Task<bool> SendToPrinterAsync(LabelPrinter printer, string zpl, CancellationToken cancellationToken = default)
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
            var data = Encoding.UTF8.GetBytes(zpl);

            await stream.WriteAsync(data, cancellationToken);
            await stream.FlushAsync(cancellationToken);

            _logger.LogInformation("Print job sent successfully to {PrinterName}: {ByteCount} bytes",
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
}
