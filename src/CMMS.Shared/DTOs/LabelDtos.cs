namespace CMMS.Shared.DTOs;

// Label Template DTOs
public class LabelTemplateDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Width { get; set; }
    public decimal Height { get; set; }
    public int Dpi { get; set; }
    public string ElementsJson { get; set; } = "[]";
    public bool IsDefault { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateLabelTemplateRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Width { get; set; } = 2.0m;
    public decimal Height { get; set; } = 1.0m;
    public int Dpi { get; set; } = 203;
    public string ElementsJson { get; set; } = "[]";
    public bool IsDefault { get; set; }
}

public class UpdateLabelTemplateRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Width { get; set; }
    public decimal Height { get; set; }
    public int Dpi { get; set; }
    public string ElementsJson { get; set; } = "[]";
    public bool IsDefault { get; set; }
}

// Label Printer DTOs
public class LabelPrinterDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    // Connection settings
    public string ConnectionType { get; set; } = "Network";  // "Network" or "WindowsPrinter"
    public string IpAddress { get; set; } = string.Empty;
    public int Port { get; set; }
    public string? WindowsPrinterName { get; set; }

    // Printer settings
    public string? PrinterModel { get; set; }
    public string Language { get; set; } = "ZPL";  // "ZPL" or "EPL"
    public int Dpi { get; set; }
    public bool IsActive { get; set; }
    public bool IsDefault { get; set; }
    public string? Location { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateLabelPrinterRequest
{
    public string Name { get; set; } = string.Empty;

    // Connection settings
    public string ConnectionType { get; set; } = "Network";
    public string IpAddress { get; set; } = string.Empty;
    public int Port { get; set; } = 9100;
    public string? WindowsPrinterName { get; set; }

    // Printer settings
    public string? PrinterModel { get; set; }
    public string Language { get; set; } = "ZPL";
    public int Dpi { get; set; } = 203;
    public bool IsActive { get; set; } = true;
    public bool IsDefault { get; set; }
    public string? Location { get; set; }
}

public class UpdateLabelPrinterRequest
{
    public string Name { get; set; } = string.Empty;

    // Connection settings
    public string ConnectionType { get; set; } = "Network";
    public string IpAddress { get; set; } = string.Empty;
    public int Port { get; set; }
    public string? WindowsPrinterName { get; set; }

    // Printer settings
    public string? PrinterModel { get; set; }
    public string Language { get; set; } = "ZPL";
    public int Dpi { get; set; }
    public bool IsActive { get; set; }
    public bool IsDefault { get; set; }
    public string? Location { get; set; }
}

// Print Request DTOs
public class PrintLabelRequest
{
    public int PartId { get; set; }
    public int? TemplateId { get; set; }
    public int? PrinterId { get; set; }
    public int Quantity { get; set; } = 1;
}

public class PrintPreviewRequest
{
    public int PartId { get; set; }
    public int? TemplateId { get; set; }
    public int? PrinterId { get; set; }  // Used to determine language (EPL/ZPL)
}

public class PrintPreviewResponse
{
    public string Zpl { get; set; } = string.Empty;  // Contains EPL or ZPL commands
    public string Language { get; set; } = "ZPL";    // "ZPL" or "EPL"
    public string TemplateName { get; set; } = string.Empty;
    public decimal Width { get; set; }
    public decimal Height { get; set; }
}

public class PrintResultDto
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? PrinterName { get; set; }
    public int LabelsPrinted { get; set; }
}

public class PrinterTestResultDto
{
    public bool Success { get; set; }
    public string? Message { get; set; }
}

// Label Element DTO (for template editor)
public class LabelElementDto
{
    public string Type { get; set; } = string.Empty;
    public string Field { get; set; } = string.Empty;
    public int X { get; set; }
    public int Y { get; set; }
    public int? FontSize { get; set; }
    public int? Height { get; set; }
    public int MaxWidth { get; set; }
    public int? BarcodeWidth { get; set; }  // Module/bar width for barcodes (1-5 dots)
    public string? Format { get; set; }
}
