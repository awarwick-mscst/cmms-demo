using CMMS.Core.Interfaces;
using CMMS.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CMMS.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class BarcodeController : ControllerBase
{
    private readonly IPartService _partService;
    private readonly IAssetService _assetService;

    public BarcodeController(IPartService partService, IAssetService assetService)
    {
        _partService = partService;
        _assetService = assetService;
    }

    [HttpGet("lookup/{barcode}")]
    public async Task<ActionResult<ApiResponse<BarcodeLookupResult>>> Lookup(
        string barcode,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(barcode))
        {
            return BadRequest(ApiResponse<BarcodeLookupResult>.Fail("Barcode is required"));
        }

        // First, try to find a Part with this barcode
        var part = await _partService.GetPartByBarcodeAsync(barcode, cancellationToken);
        if (part != null)
        {
            return Ok(ApiResponse<BarcodeLookupResult>.Ok(new BarcodeLookupResult
            {
                Type = "Part",
                Id = part.Id,
                Name = part.Name,
                Code = part.PartNumber,
                Description = part.Description,
                Barcode = part.Barcode ?? barcode
            }));
        }

        // Next, try to find an Asset with this barcode
        var asset = await _assetService.GetAssetByBarcodeAsync(barcode, cancellationToken);
        if (asset != null)
        {
            return Ok(ApiResponse<BarcodeLookupResult>.Ok(new BarcodeLookupResult
            {
                Type = "Asset",
                Id = asset.Id,
                Name = asset.Name,
                Code = asset.AssetTag,
                Description = asset.Description,
                Barcode = asset.Barcode ?? barcode
            }));
        }

        return NotFound(ApiResponse<BarcodeLookupResult>.Fail("No part or asset found with this barcode"));
    }
}
