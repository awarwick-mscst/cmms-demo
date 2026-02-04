using System.Text;
using System.Text.Json;
using CMMS.Core.Entities;

namespace CMMS.Infrastructure.Services;

/// <summary>
/// Generates EPL (Eltron Programming Language) commands for older Zebra printers
/// like the LP 2824. EPL uses a coordinate system where (0,0) is the top-left corner.
/// </summary>
public class EplGenerator
{
    public string GenerateLabel(Part part, LabelTemplate template)
    {
        var sb = new StringBuilder();

        // EPL2 commands use CR (\r) or CR+LF as line terminator
        // Clear image buffer
        sb.Append("N\r\n");

        // Set label width and height in dots
        var widthDots = (int)(template.Width * template.Dpi);
        var heightDots = (int)(template.Height * template.Dpi);

        // Q command sets label height (in dots) and gap (typically 24 dots for 1/8" gap)
        sb.Append($"Q{heightDots},24\r\n");

        // q command sets label width
        sb.Append($"q{widthDots}\r\n");

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
                    sb.Append(GenerateTextElement(element, value));
                    sb.Append("\r\n");
                    break;
                case "barcode":
                    sb.Append(GenerateBarcodeElement(element, value));
                    sb.Append("\r\n");
                    break;
            }
        }

        // Print 1 label
        sb.Append("P1\r\n");

        return sb.ToString();
    }

    /// <summary>
    /// Generate a simple test label to verify printer connectivity
    /// </summary>
    public string GenerateTestLabel(int dpi = 203)
    {
        var sb = new StringBuilder();

        // Clear buffer
        sb.Append("N\r\n");

        // Set label size for 2" x 1" label
        var widthDots = 2 * dpi;
        var heightDots = 1 * dpi;
        sb.Append($"Q{heightDots},24\r\n");
        sb.Append($"q{widthDots}\r\n");

        // Print text
        sb.Append("A10,10,0,3,1,1,N,\"CMMS Test Label\"\r\n");
        sb.Append("A10,50,0,2,1,1,N,\"Printer OK\"\r\n");

        // Print a test barcode
        sb.Append("B10,90,0,1,2,4,50,B,\"TEST123\"\r\n");

        // Print 1 label
        sb.Append("P1\r\n");

        return sb.ToString();
    }

    private string GenerateTextElement(LabelElement element, string value)
    {
        // EPL Text format: A x,y,rotation,font,h_mult,v_mult,reverse,"data"
        // rotation: 0=0°, 1=90°, 2=180°, 3=270°
        // font: 1-5 are built-in fonts (1=smallest, 5=largest)
        // h_mult, v_mult: horizontal/vertical multipliers (1-8)

        var font = GetEplFont(element.FontSize ?? 25);
        var multiplier = GetFontMultiplier(element.FontSize ?? 25);

        // Escape quotes in the value
        var escapedValue = EscapeEplText(value);

        // Truncate if maxWidth specified (rough character estimate)
        if (element.MaxWidth > 0)
        {
            var charWidth = GetApproxCharWidth(font, multiplier);
            var maxChars = element.MaxWidth / charWidth;
            if (escapedValue.Length > maxChars)
            {
                escapedValue = escapedValue.Substring(0, (int)maxChars - 3) + "...";
            }
        }

        return $"A{element.X},{element.Y},0,{font},{multiplier},{multiplier},N,\"{escapedValue}\"";
    }

    private string GenerateBarcodeElement(LabelElement element, string value)
    {
        // EPL Barcode format: B x,y,rotation,barcode_type,narrow_bar,wide_bar,height,print_human,"data"
        // barcode_type: 1=Code 128, 3=Code 39, etc.
        // narrow_bar, wide_bar: bar widths in dots
        // height: barcode height in dots
        // print_human: B=yes, N=no

        var height = element.Height ?? 50;
        var narrowBar = element.BarcodeWidth ?? 2;
        var wideBar = narrowBar * 2;  // Wide bar is typically 2x narrow bar
        var escapedValue = EscapeEplText(value);

        // Use Code 128 (type 1)
        // B = print human-readable text below barcode
        return $"B{element.X},{element.Y},0,1,{narrowBar},{wideBar},{height},B,\"{escapedValue}\"";
    }

    private int GetEplFont(int fontSize)
    {
        // Map approximate font sizes to EPL built-in fonts
        // Font 1: 8x12 dots, Font 2: 10x16 dots, Font 3: 12x20 dots
        // Font 4: 14x32 dots, Font 5: 32x48 dots
        return fontSize switch
        {
            < 15 => 1,
            < 20 => 2,
            < 30 => 3,
            < 45 => 4,
            _ => 5
        };
    }

    private int GetFontMultiplier(int fontSize)
    {
        // Use multipliers for larger sizes
        return fontSize switch
        {
            < 15 => 1,
            < 25 => 1,
            < 35 => 2,
            < 50 => 2,
            < 70 => 3,
            _ => 4
        };
    }

    private int GetApproxCharWidth(int font, int multiplier)
    {
        // Approximate character widths for EPL fonts
        var baseWidth = font switch
        {
            1 => 8,
            2 => 10,
            3 => 12,
            4 => 14,
            5 => 32,
            _ => 10
        };
        return baseWidth * multiplier;
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

    private string EscapeEplText(string text)
    {
        // EPL uses quotes for strings, escape quotes with backslash
        return text.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }
}
