using CMMS.Core.Entities;
using CMMS.Core.Interfaces;
using CMMS.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CMMS.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class AssetLocationsController : ControllerBase
{
    private readonly IAssetLocationService _locationService;
    private readonly ICurrentUserService _currentUserService;

    public AssetLocationsController(IAssetLocationService locationService, ICurrentUserService currentUserService)
    {
        _locationService = locationService;
        _currentUserService = currentUserService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<AssetLocationDto>>>> GetLocations(
        [FromQuery] bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        var locations = await _locationService.GetLocationsAsync(includeInactive, cancellationToken);
        var dtos = locations.Select(MapToDto);

        return Ok(ApiResponse<IEnumerable<AssetLocationDto>>.Ok(dtos));
    }

    [HttpGet("tree")]
    public async Task<ActionResult<ApiResponse<IEnumerable<AssetLocationDto>>>> GetLocationTree(CancellationToken cancellationToken = default)
    {
        var locations = await _locationService.GetLocationTreeAsync(cancellationToken);
        var dtos = locations.Select(MapToDto);

        return Ok(ApiResponse<IEnumerable<AssetLocationDto>>.Ok(dtos));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<AssetLocationDto>>> GetLocation(int id, CancellationToken cancellationToken = default)
    {
        var location = await _locationService.GetLocationByIdAsync(id, cancellationToken);

        if (location == null)
        {
            return NotFound(ApiResponse<AssetLocationDto>.Fail("Location not found"));
        }

        return Ok(ApiResponse<AssetLocationDto>.Ok(MapToDto(location)));
    }

    [HttpPost]
    [Authorize(Policy = "CanCreateAssetLocations")]
    public async Task<ActionResult<ApiResponse<AssetLocationDto>>> CreateLocation(
        [FromBody] CreateAssetLocationRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue)
        {
            return Unauthorized(ApiResponse<AssetLocationDto>.Fail("User not authenticated"));
        }

        var location = new AssetLocation
        {
            Name = request.Name,
            Code = request.Code,
            Description = request.Description,
            ParentId = request.ParentId,
            Building = request.Building,
            Floor = request.Floor,
            Room = request.Room
        };

        var created = await _locationService.CreateLocationAsync(location, userId.Value, cancellationToken);

        return CreatedAtAction(
            nameof(GetLocation),
            new { id = created.Id },
            ApiResponse<AssetLocationDto>.Ok(MapToDto(created), "Location created successfully"));
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "CanEditAssetLocations")]
    public async Task<ActionResult<ApiResponse<AssetLocationDto>>> UpdateLocation(
        int id,
        [FromBody] CreateAssetLocationRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue)
        {
            return Unauthorized(ApiResponse<AssetLocationDto>.Fail("User not authenticated"));
        }

        var location = await _locationService.GetLocationByIdAsync(id, cancellationToken);
        if (location == null)
        {
            return NotFound(ApiResponse<AssetLocationDto>.Fail("Location not found"));
        }

        location.Name = request.Name;
        location.Code = request.Code;
        location.Description = request.Description;
        location.ParentId = request.ParentId;
        location.Building = request.Building;
        location.Floor = request.Floor;
        location.Room = request.Room;

        var updated = await _locationService.UpdateLocationAsync(location, userId.Value, cancellationToken);

        return Ok(ApiResponse<AssetLocationDto>.Ok(MapToDto(updated), "Location updated successfully"));
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "CanDeleteAssetLocations")]
    public async Task<ActionResult<ApiResponse>> DeleteLocation(int id, CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue)
        {
            return Unauthorized(ApiResponse.Fail("User not authenticated"));
        }

        var success = await _locationService.DeleteLocationAsync(id, userId.Value, cancellationToken);

        if (!success)
        {
            return BadRequest(ApiResponse.Fail("Cannot delete location. It may have assets or not exist."));
        }

        return Ok(ApiResponse.Ok("Location deleted successfully"));
    }

    private static AssetLocationDto MapToDto(AssetLocation location)
    {
        return new AssetLocationDto
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
            Floor = location.Floor,
            Room = location.Room,
            IsActive = location.IsActive,
            Children = location.Children
                .Where(l => !l.IsDeleted && l.IsActive)
                .OrderBy(l => l.Name)
                .Select(MapToDto)
                .ToList()
        };
    }
}
