using CMMS.Core.Entities;
using CMMS.Core.Interfaces;
using CMMS.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CMMS.API.Controllers;

[ApiController]
[Route("api/v1/storage-locations")]
[Authorize]
public class StorageLocationsController : ControllerBase
{
    private readonly IStorageLocationService _locationService;
    private readonly ICurrentUserService _currentUserService;

    public StorageLocationsController(IStorageLocationService locationService, ICurrentUserService currentUserService)
    {
        _locationService = locationService;
        _currentUserService = currentUserService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<StorageLocationDto>>>> GetLocations(
        [FromQuery] bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        var locations = await _locationService.GetLocationTreeAsync(includeInactive, cancellationToken);
        var dtos = locations.Select(l => MapToDto(l));
        return Ok(ApiResponse<IEnumerable<StorageLocationDto>>.Ok(dtos));
    }

    [HttpGet("flat")]
    public async Task<ActionResult<ApiResponse<IEnumerable<StorageLocationDto>>>> GetLocationsFlat(
        [FromQuery] bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        var locations = await _locationService.GetLocationsAsync(includeInactive, cancellationToken);
        var dtos = locations.Select(l => MapToDto(l, false));
        return Ok(ApiResponse<IEnumerable<StorageLocationDto>>.Ok(dtos));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<StorageLocationDto>>> GetLocation(int id, CancellationToken cancellationToken = default)
    {
        var location = await _locationService.GetLocationByIdAsync(id, cancellationToken);

        if (location == null)
            return NotFound(ApiResponse<StorageLocationDto>.Fail("Storage location not found"));

        return Ok(ApiResponse<StorageLocationDto>.Ok(MapToDto(location)));
    }

    [HttpPost]
    [Authorize(Policy = "CanManageInventory")]
    public async Task<ActionResult<ApiResponse<StorageLocationDto>>> CreateLocation(
        [FromBody] CreateStorageLocationRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue)
            return Unauthorized(ApiResponse<StorageLocationDto>.Fail("User not authenticated"));

        if (!string.IsNullOrEmpty(request.Code) && await _locationService.CodeExistsAsync(request.Code, null, cancellationToken))
            return BadRequest(ApiResponse<StorageLocationDto>.Fail("Location code already exists"));

        var location = new StorageLocation
        {
            Name = request.Name,
            Code = request.Code,
            Description = request.Description,
            ParentId = request.ParentId,
            Building = request.Building,
            Aisle = request.Aisle,
            Rack = request.Rack,
            Shelf = request.Shelf,
            Bin = request.Bin,
            IsActive = request.IsActive
        };

        var created = await _locationService.CreateLocationAsync(location, userId.Value, cancellationToken);
        var result = await _locationService.GetLocationByIdAsync(created.Id, cancellationToken);

        return CreatedAtAction(
            nameof(GetLocation),
            new { id = created.Id },
            ApiResponse<StorageLocationDto>.Ok(MapToDto(result!), "Storage location created successfully"));
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "CanManageInventory")]
    public async Task<ActionResult<ApiResponse<StorageLocationDto>>> UpdateLocation(
        int id,
        [FromBody] UpdateStorageLocationRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue)
            return Unauthorized(ApiResponse<StorageLocationDto>.Fail("User not authenticated"));

        var location = await _locationService.GetLocationByIdAsync(id, cancellationToken);
        if (location == null)
            return NotFound(ApiResponse<StorageLocationDto>.Fail("Storage location not found"));

        if (!string.IsNullOrEmpty(request.Code) && await _locationService.CodeExistsAsync(request.Code, id, cancellationToken))
            return BadRequest(ApiResponse<StorageLocationDto>.Fail("Location code already exists"));

        if (request.ParentId == id)
            return BadRequest(ApiResponse<StorageLocationDto>.Fail("Location cannot be its own parent"));

        location.Name = request.Name;
        location.Code = request.Code;
        location.Description = request.Description;
        location.ParentId = request.ParentId;
        location.Building = request.Building;
        location.Aisle = request.Aisle;
        location.Rack = request.Rack;
        location.Shelf = request.Shelf;
        location.Bin = request.Bin;
        location.IsActive = request.IsActive;

        await _locationService.UpdateLocationAsync(location, userId.Value, cancellationToken);
        var result = await _locationService.GetLocationByIdAsync(id, cancellationToken);

        return Ok(ApiResponse<StorageLocationDto>.Ok(MapToDto(result!), "Storage location updated successfully"));
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "CanManageInventory")]
    public async Task<ActionResult<ApiResponse>> DeleteLocation(int id, CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue)
            return Unauthorized(ApiResponse.Fail("User not authenticated"));

        if (await _locationService.HasStockAsync(id, cancellationToken))
            return BadRequest(ApiResponse.Fail("Cannot delete location with stock items"));

        var success = await _locationService.DeleteLocationAsync(id, userId.Value, cancellationToken);

        if (!success)
            return NotFound(ApiResponse.Fail("Storage location not found"));

        return Ok(ApiResponse.Ok("Storage location deleted successfully"));
    }

    private static StorageLocationDto MapToDto(StorageLocation location, bool includeChildren = true)
    {
        return new StorageLocationDto
        {
            Id = location.Id,
            Name = location.Name,
            Code = location.Code,
            Description = location.Description,
            ParentId = location.ParentId,
            ParentName = location.Parent?.Name,
            Level = location.Level,
            FullPath = location.FullPath,
            Building = location.Building,
            Aisle = location.Aisle,
            Rack = location.Rack,
            Shelf = location.Shelf,
            Bin = location.Bin,
            IsActive = location.IsActive,
            StockItemCount = location.PartStocks?.Count ?? 0,
            Children = includeChildren ? location.Children?.Select(c => MapToDto(c, true)).ToList() ?? new List<StorageLocationDto>() : new List<StorageLocationDto>(),
            CreatedAt = location.CreatedAt,
            UpdatedAt = location.UpdatedAt
        };
    }
}
