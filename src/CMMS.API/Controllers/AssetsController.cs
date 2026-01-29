using CMMS.Core.Entities;
using CMMS.Core.Enums;
using CMMS.Core.Interfaces;
using CMMS.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CMMS.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class AssetsController : ControllerBase
{
    private readonly IAssetService _assetService;
    private readonly ICurrentUserService _currentUserService;

    public AssetsController(IAssetService assetService, ICurrentUserService currentUserService)
    {
        _assetService = assetService;
        _currentUserService = currentUserService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResponse<AssetDto>>> GetAssets(
        [FromQuery] string? search,
        [FromQuery] int? categoryId,
        [FromQuery] int? locationId,
        [FromQuery] string? status,
        [FromQuery] string? criticality,
        [FromQuery] int? assignedTo,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? sortBy = null,
        [FromQuery] bool sortDescending = false,
        CancellationToken cancellationToken = default)
    {
        var filter = new AssetFilter
        {
            Search = search,
            CategoryId = categoryId,
            LocationId = locationId,
            Status = status,
            Criticality = criticality,
            AssignedTo = assignedTo,
            Page = page,
            PageSize = Math.Min(pageSize, 100),
            SortBy = sortBy,
            SortDescending = sortDescending
        };

        var result = await _assetService.GetAssetsAsync(filter, cancellationToken);

        var response = new PagedResponse<AssetDto>
        {
            Items = result.Items.Select(MapToDto),
            TotalCount = result.TotalCount,
            Page = result.Page,
            PageSize = result.PageSize,
            TotalPages = result.TotalPages,
            HasPreviousPage = result.HasPreviousPage,
            HasNextPage = result.HasNextPage
        };

        return Ok(response);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<AssetDto>>> GetAsset(int id, CancellationToken cancellationToken = default)
    {
        var asset = await _assetService.GetAssetByIdAsync(id, cancellationToken);

        if (asset == null)
        {
            return NotFound(ApiResponse<AssetDto>.Fail("Asset not found"));
        }

        return Ok(ApiResponse<AssetDto>.Ok(MapToDto(asset)));
    }

    [HttpPost]
    [Authorize(Policy = "CanCreateAssets")]
    public async Task<ActionResult<ApiResponse<AssetDto>>> CreateAsset(
        [FromBody] CreateAssetRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue)
        {
            return Unauthorized(ApiResponse<AssetDto>.Fail("User not authenticated"));
        }

        var asset = new Asset
        {
            AssetTag = request.AssetTag ?? string.Empty,
            Name = request.Name,
            Description = request.Description,
            CategoryId = request.CategoryId,
            LocationId = request.LocationId,
            Status = Enum.Parse<AssetStatus>(request.Status),
            Criticality = Enum.Parse<AssetCriticality>(request.Criticality),
            Manufacturer = request.Manufacturer,
            Model = request.Model,
            SerialNumber = request.SerialNumber,
            Barcode = request.Barcode,
            PurchaseDate = request.PurchaseDate,
            PurchaseCost = request.PurchaseCost,
            WarrantyExpiry = request.WarrantyExpiry,
            ExpectedLifeYears = request.ExpectedLifeYears,
            InstallationDate = request.InstallationDate,
            ParentAssetId = request.ParentAssetId,
            AssignedTo = request.AssignedTo,
            Notes = request.Notes
        };

        var created = await _assetService.CreateAssetAsync(asset, userId.Value, cancellationToken);

        // Reload with related data
        var result = await _assetService.GetAssetByIdAsync(created.Id, cancellationToken);

        return CreatedAtAction(
            nameof(GetAsset),
            new { id = created.Id },
            ApiResponse<AssetDto>.Ok(MapToDto(result!), "Asset created successfully"));
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "CanEditAssets")]
    public async Task<ActionResult<ApiResponse<AssetDto>>> UpdateAsset(
        int id,
        [FromBody] UpdateAssetRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue)
        {
            return Unauthorized(ApiResponse<AssetDto>.Fail("User not authenticated"));
        }

        var asset = await _assetService.GetAssetByIdAsync(id, cancellationToken);
        if (asset == null)
        {
            return NotFound(ApiResponse<AssetDto>.Fail("Asset not found"));
        }

        asset.Name = request.Name;
        asset.Description = request.Description;
        asset.CategoryId = request.CategoryId;
        asset.LocationId = request.LocationId;
        asset.Status = Enum.Parse<AssetStatus>(request.Status);
        asset.Criticality = Enum.Parse<AssetCriticality>(request.Criticality);
        asset.Manufacturer = request.Manufacturer;
        asset.Model = request.Model;
        asset.SerialNumber = request.SerialNumber;
        asset.Barcode = request.Barcode;
        asset.PurchaseDate = request.PurchaseDate;
        asset.PurchaseCost = request.PurchaseCost;
        asset.WarrantyExpiry = request.WarrantyExpiry;
        asset.ExpectedLifeYears = request.ExpectedLifeYears;
        asset.InstallationDate = request.InstallationDate;
        asset.LastMaintenanceDate = request.LastMaintenanceDate;
        asset.NextMaintenanceDate = request.NextMaintenanceDate;
        asset.ParentAssetId = request.ParentAssetId;
        asset.AssignedTo = request.AssignedTo;
        asset.Notes = request.Notes;

        var updated = await _assetService.UpdateAssetAsync(asset, userId.Value, cancellationToken);

        // Reload with related data
        var result = await _assetService.GetAssetByIdAsync(updated.Id, cancellationToken);

        return Ok(ApiResponse<AssetDto>.Ok(MapToDto(result!), "Asset updated successfully"));
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "CanDeleteAssets")]
    public async Task<ActionResult<ApiResponse>> DeleteAsset(int id, CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue)
        {
            return Unauthorized(ApiResponse.Fail("User not authenticated"));
        }

        var success = await _assetService.DeleteAssetAsync(id, userId.Value, cancellationToken);

        if (!success)
        {
            return NotFound(ApiResponse.Fail("Asset not found"));
        }

        return Ok(ApiResponse.Ok("Asset deleted successfully"));
    }

    private static AssetDto MapToDto(Asset asset)
    {
        return new AssetDto
        {
            Id = asset.Id,
            AssetTag = asset.AssetTag,
            Name = asset.Name,
            Description = asset.Description,
            CategoryId = asset.CategoryId,
            CategoryName = asset.Category?.Name ?? string.Empty,
            LocationId = asset.LocationId,
            LocationName = asset.Location?.FullPath ?? asset.Location?.Name,
            Status = asset.Status.ToString(),
            Criticality = asset.Criticality.ToString(),
            Manufacturer = asset.Manufacturer,
            Model = asset.Model,
            SerialNumber = asset.SerialNumber,
            Barcode = asset.Barcode,
            PurchaseDate = asset.PurchaseDate,
            PurchaseCost = asset.PurchaseCost,
            WarrantyExpiry = asset.WarrantyExpiry,
            ExpectedLifeYears = asset.ExpectedLifeYears,
            InstallationDate = asset.InstallationDate,
            LastMaintenanceDate = asset.LastMaintenanceDate,
            NextMaintenanceDate = asset.NextMaintenanceDate,
            ParentAssetId = asset.ParentAssetId,
            ParentAssetName = asset.ParentAsset?.Name,
            AssignedTo = asset.AssignedTo,
            AssignedToName = asset.AssignedUser?.FullName,
            Notes = asset.Notes,
            CreatedAt = asset.CreatedAt,
            UpdatedAt = asset.UpdatedAt
        };
    }
}
