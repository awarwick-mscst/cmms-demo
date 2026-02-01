using CMMS.Core.Entities;
using CMMS.Core.Interfaces;
using CMMS.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CMMS.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class PrintersController : ControllerBase
{
    private readonly ILabelService _labelService;
    private readonly IPrintService _printService;

    public PrintersController(ILabelService labelService, IPrintService printService)
    {
        _labelService = labelService;
        _printService = printService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<LabelPrinterDto>>>> GetPrinters(
        [FromQuery] bool activeOnly = false,
        CancellationToken cancellationToken = default)
    {
        var printers = await _labelService.GetPrintersAsync(activeOnly, cancellationToken);
        var dtos = printers.Select(MapToDto);
        return Ok(ApiResponse<IEnumerable<LabelPrinterDto>>.Ok(dtos));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<LabelPrinterDto>>> GetPrinter(
        int id,
        CancellationToken cancellationToken = default)
    {
        var printer = await _labelService.GetPrinterByIdAsync(id, cancellationToken);

        if (printer == null)
            return NotFound(ApiResponse<LabelPrinterDto>.Fail("Printer not found"));

        return Ok(ApiResponse<LabelPrinterDto>.Ok(MapToDto(printer)));
    }

    [HttpGet("default")]
    public async Task<ActionResult<ApiResponse<LabelPrinterDto>>> GetDefaultPrinter(
        CancellationToken cancellationToken = default)
    {
        var printer = await _labelService.GetDefaultPrinterAsync(cancellationToken);

        if (printer == null)
            return NotFound(ApiResponse<LabelPrinterDto>.Fail("No default printer configured"));

        return Ok(ApiResponse<LabelPrinterDto>.Ok(MapToDto(printer)));
    }

    [HttpPost]
    [Authorize(Policy = "CanManageLabels")]
    public async Task<ActionResult<ApiResponse<LabelPrinterDto>>> CreatePrinter(
        [FromBody] CreateLabelPrinterRequest request,
        CancellationToken cancellationToken = default)
    {
        var printer = new LabelPrinter
        {
            Name = request.Name,
            IpAddress = request.IpAddress,
            Port = request.Port,
            PrinterModel = request.PrinterModel,
            Dpi = request.Dpi,
            IsActive = request.IsActive,
            IsDefault = request.IsDefault,
            Location = request.Location
        };

        var created = await _labelService.CreatePrinterAsync(printer, cancellationToken);

        return CreatedAtAction(
            nameof(GetPrinter),
            new { id = created.Id },
            ApiResponse<LabelPrinterDto>.Ok(MapToDto(created), "Printer created successfully"));
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "CanManageLabels")]
    public async Task<ActionResult<ApiResponse<LabelPrinterDto>>> UpdatePrinter(
        int id,
        [FromBody] UpdateLabelPrinterRequest request,
        CancellationToken cancellationToken = default)
    {
        var printer = await _labelService.GetPrinterByIdAsync(id, cancellationToken);
        if (printer == null)
            return NotFound(ApiResponse<LabelPrinterDto>.Fail("Printer not found"));

        printer.Name = request.Name;
        printer.IpAddress = request.IpAddress;
        printer.Port = request.Port;
        printer.PrinterModel = request.PrinterModel;
        printer.Dpi = request.Dpi;
        printer.IsActive = request.IsActive;
        printer.IsDefault = request.IsDefault;
        printer.Location = request.Location;

        var updated = await _labelService.UpdatePrinterAsync(printer, cancellationToken);

        return Ok(ApiResponse<LabelPrinterDto>.Ok(MapToDto(updated), "Printer updated successfully"));
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "CanManageLabels")]
    public async Task<ActionResult<ApiResponse>> DeletePrinter(
        int id,
        CancellationToken cancellationToken = default)
    {
        var success = await _labelService.DeletePrinterAsync(id, cancellationToken);

        if (!success)
            return NotFound(ApiResponse.Fail("Printer not found"));

        return Ok(ApiResponse.Ok("Printer deleted successfully"));
    }

    [HttpPost("{id}/test")]
    [Authorize(Policy = "CanManageLabels")]
    public async Task<ActionResult<ApiResponse<PrinterTestResultDto>>> TestPrinter(
        int id,
        CancellationToken cancellationToken = default)
    {
        var printer = await _labelService.GetPrinterByIdAsync(id, cancellationToken);
        if (printer == null)
            return NotFound(ApiResponse<PrinterTestResultDto>.Fail("Printer not found"));

        var success = await _printService.TestPrinterConnectionAsync(printer, cancellationToken);

        var result = new PrinterTestResultDto
        {
            Success = success,
            Message = success
                ? $"Successfully connected to {printer.Name} at {printer.IpAddress}:{printer.Port}"
                : $"Failed to connect to {printer.Name} at {printer.IpAddress}:{printer.Port}"
        };

        return Ok(ApiResponse<PrinterTestResultDto>.Ok(result));
    }

    [HttpPost("{id}/set-default")]
    [Authorize(Policy = "CanManageLabels")]
    public async Task<ActionResult<ApiResponse>> SetDefaultPrinter(
        int id,
        CancellationToken cancellationToken = default)
    {
        var success = await _labelService.SetDefaultPrinterAsync(id, cancellationToken);

        if (!success)
            return NotFound(ApiResponse.Fail("Printer not found"));

        return Ok(ApiResponse.Ok("Default printer set successfully"));
    }

    private static LabelPrinterDto MapToDto(LabelPrinter printer)
    {
        return new LabelPrinterDto
        {
            Id = printer.Id,
            Name = printer.Name,
            IpAddress = printer.IpAddress,
            Port = printer.Port,
            PrinterModel = printer.PrinterModel,
            Dpi = printer.Dpi,
            IsActive = printer.IsActive,
            IsDefault = printer.IsDefault,
            Location = printer.Location,
            CreatedAt = printer.CreatedAt,
            UpdatedAt = printer.UpdatedAt
        };
    }
}
