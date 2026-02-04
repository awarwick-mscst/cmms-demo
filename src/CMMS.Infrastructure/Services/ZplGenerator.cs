using System.Text;
using System.Text.Json;
using CMMS.Core.Entities;

namespace CMMS.Infrastructure.Services;

public class ZplGenerator
{
    public string GenerateLabel(Part part, LabelTemplate template)
    {
        var sb = new StringBuilder();

        // Start label
        sb.AppendLine("^XA");

        // Set label size in dots
        var widthDots = (int)(template.Width * template.Dpi);
        var heightDots = (int)(template.Height * template.Dpi);
        sb.AppendLine($"^PW{widthDots}");
        sb.AppendLine($"^LL{heightDots}");

        // Parse elements from JSON
        var elements = ParseElements(template.ElementsJson);

        foreach (var element in elements)
        {
            var value = GetFieldValue(part, element.Field);
            if (string.IsNullOrEmpty(value))
                continue;

            switch (element.Type?.ToLower())
            {
                case "text":
                    sb.AppendLine(GenerateTextElement(element, value));
                    break;
                case "barcode":
                    sb.AppendLine(GenerateBarcodeElement(element, value));
                    break;
            }
        }

        // End label
        sb.AppendLine("^XZ");

        return sb.ToString();
    }

    private string GenerateTextElement(LabelElement element, string value)
    {
        var sb = new StringBuilder();

        // Field origin
        sb.Append($"^FO{element.X},{element.Y}");

        // Font (A0 = default scalable font, N = normal orientation)
        var fontSize = element.FontSize ?? 25;
        sb.Append($"^A0N,{fontSize},{fontSize}");

        // Field block for text wrapping if maxWidth specified
        if (element.MaxWidth > 0)
        {
            var maxLines = 2;
            sb.Append($"^FB{element.MaxWidth},{maxLines},0,L");
        }

        // Escape special characters in value
        var escapedValue = EscapeZplText(value);

        // Field data
        sb.Append($"^FD{escapedValue}^FS");

        return sb.ToString();
    }

    private string GenerateBarcodeElement(LabelElement element, string value)
    {
        var sb = new StringBuilder();

        // Field origin
        sb.Append($"^FO{element.X},{element.Y}");

        // Bar code module width (1-10, higher = thicker bars)
        var moduleWidth = element.BarcodeWidth ?? 2;
        sb.Append($"^BY{moduleWidth}");

        // Code 128 barcode
        // ^BC = Code 128, orientation, height, interpretation line, interpretation above, UCC check digit
        var height = element.Height ?? 50;
        sb.Append($"^BCN,{height},Y,N,N");

        // Escape special characters
        var escapedValue = EscapeZplText(value);

        // Field data
        sb.Append($"^FD{escapedValue}^FS");

        return sb.ToString();
    }

    private List<LabelElement> ParseElements(string elementsJson)
    {
        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            return JsonSerializer.Deserialize<List<LabelElement>>(elementsJson, options) ?? new List<LabelElement>();
        }
        catch
        {
            return new List<LabelElement>();
        }
    }

    private string GetFieldValue(Part part, string? field)
    {
        return field?.ToLower() switch
        {
            "description" => part.Description ?? part.Name,
            "partnumber" => part.PartNumber,
            "manufacturerpartnumber" => part.ManufacturerPartNumber ?? string.Empty,
            "name" => part.Name,
            "manufacturer" => part.Manufacturer ?? string.Empty,
            "barcode" => part.Barcode ?? part.PartNumber,
            _ => string.Empty
        };
    }

    private string EscapeZplText(string text)
    {
        // ZPL uses backslash as escape character
        // Escape special ZPL characters
        return text
            .Replace("\\", "\\\\")
            .Replace("^", "\\^")
            .Replace("~", "\\~");
    }
}

public class LabelElement
{
    public string? Type { get; set; }
    public string? Field { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public int? FontSize { get; set; }
    public int? Height { get; set; }
    public int MaxWidth { get; set; }
    public int? BarcodeWidth { get; set; }  // Module/bar width for barcodes (1-5 dots)
    public string? Format { get; set; }
}
