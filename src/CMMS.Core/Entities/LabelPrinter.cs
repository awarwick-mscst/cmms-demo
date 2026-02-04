namespace CMMS.Core.Entities;

public enum PrinterLanguage
{
    ZPL,  // Zebra Programming Language (newer printers)
    EPL   // Eltron Programming Language (older printers like LP 2824)
}

public enum PrinterConnectionType
{
    Network,       // TCP/IP connection (port 9100)
    WindowsPrinter // Windows shared printer (USB, parallel, etc.)
}

public class LabelPrinter
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    // Connection settings
    public PrinterConnectionType ConnectionType { get; set; } = PrinterConnectionType.Network;
    public string IpAddress { get; set; } = string.Empty;  // For Network connection
    public int Port { get; set; } = 9100;                   // For Network connection
    public string? WindowsPrinterName { get; set; }         // For WindowsPrinter connection

    // Printer settings
    public string? PrinterModel { get; set; }
    public PrinterLanguage Language { get; set; } = PrinterLanguage.ZPL;
    public int Dpi { get; set; } = 203;
    public bool IsActive { get; set; } = true;
    public bool IsDefault { get; set; }
    public string? Location { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
}
