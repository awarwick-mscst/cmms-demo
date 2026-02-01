using CMMS.Core.Interfaces;
using CMMS.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CMMS.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class PrintController : ControllerBase
{
    private readonly ILabelService _labelService;
    private readonly IPrintService _printService;
    private readonly IPartService _partService;

    public PrintController(
        ILabelService labelService,
        IPrintService printService,
        IPartService partService)
    {
        _labelService = labelService;
        _printService = printService;
        _partService = partService;
    }

    [HttpPost("part-label")]
    [Authorize(Policy = "CanPrintLabels")]
    public async Task<ActionResult<ApiResponse<PrintResultDto>>> PrintPartLabel(
        [FromBody] PrintLabelRequest request,
        CancellationToken cancellationToken = default)
    {
        // Get part
        var part = await _partService.GetPartByIdAsync(request.PartId, cancellationToken);
        if (part == null)
            return NotFound(ApiResponse<PrintResultDto>.Fail("Part not found"));

        // Get template (use specified or default)
        var template = request.TemplateId.HasValue
            ? await _labelService.GetTemplateByIdAsync(request.TemplateId.Value, cancellationToken)
            : await _labelService.GetDefaultTemplateAsync(cancellationToken);

        if (template == null)
            return BadRequest(ApiResponse<PrintResultDto>.Fail("Label template not found or no default template configured"));

        // Get printer (use specified or default)
        var printer = request.PrinterId.HasValue
            ? await _labelService.GetPrinterByIdAsync(request.PrinterId.Value, cancellationToken)
            : await _labelService.GetDefaultPrinterAsync(cancellationToken);

        if (printer == null)
            return BadRequest(ApiResponse<PrintResultDto>.Fail("Printer not found or no default printer configured"));

        if (!printer.IsActive)
            return BadRequest(ApiResponse<PrintResultDto>.Fail("Selected printer is not active"));

        // Print labels
        var quantity = Math.Max(1, Math.Min(request.Quantity, 100)); // Limit to 100 labels
        var success = await _printService.PrintLabelAsync(part, template, printer, quantity, cancellationToken);

        var result = new PrintResultDto
        {
            Success = success,
            Message = success
                ? $"Successfully printed {quantity} label(s) to {printer.Name}"
                : $"Failed to print to {printer.Name}",
            PrinterName = printer.Name,
            LabelsPrinted = success ? quantity : 0
        };

        if (!success)
            return StatusCode(500, ApiResponse<PrintResultDto>.Fail(result.Message));

        return Ok(ApiResponse<PrintResultDto>.Ok(result));
    }

    [HttpPost("preview")]
    [Authorize(Policy = "CanPrintLabels")]
    public async Task<ActionResult<ApiResponse<PrintPreviewResponse>>> GetPrintPreview(
        [FromBody] PrintPreviewRequest request,
        CancellationToken cancellationToken = default)
    {
        // Get part
        var part = await _partService.GetPartByIdAsync(request.PartId, cancellationToken);
        if (part == null)
            return NotFound(ApiResponse<PrintPreviewResponse>.Fail("Part not found"));

        // Get template (use specified or default)
        var template = request.TemplateId.HasValue
            ? await _labelService.GetTemplateByIdAsync(request.TemplateId.Value, cancellationToken)
            : await _labelService.GetDefaultTemplateAsync(cancellationToken);

        if (template == null)
            return BadRequest(ApiResponse<PrintPreviewResponse>.Fail("Label template not found or no default template configured"));

        // Generate ZPL preview
        var zpl = await _printService.GenerateZplAsync(part, template, cancellationToken);

        var response = new PrintPreviewResponse
        {
            Zpl = zpl,
            TemplateName = template.Name,
            Width = template.Width,
            Height = template.Height
        };

        return Ok(ApiResponse<PrintPreviewResponse>.Ok(response));
    }
}
