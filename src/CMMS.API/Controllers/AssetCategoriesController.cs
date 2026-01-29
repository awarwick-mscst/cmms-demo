using CMMS.Core.Entities;
using CMMS.Core.Interfaces;
using CMMS.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CMMS.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class AssetCategoriesController : ControllerBase
{
    private readonly IAssetCategoryService _categoryService;
    private readonly ICurrentUserService _currentUserService;

    public AssetCategoriesController(IAssetCategoryService categoryService, ICurrentUserService currentUserService)
    {
        _categoryService = categoryService;
        _currentUserService = currentUserService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<AssetCategoryDto>>>> GetCategories(
        [FromQuery] bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        var categories = await _categoryService.GetCategoriesAsync(includeInactive, cancellationToken);
        var dtos = categories.Where(c => c.ParentId == null).Select(MapToDto);

        return Ok(ApiResponse<IEnumerable<AssetCategoryDto>>.Ok(dtos));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<AssetCategoryDto>>> GetCategory(int id, CancellationToken cancellationToken = default)
    {
        var category = await _categoryService.GetCategoryByIdAsync(id, cancellationToken);

        if (category == null)
        {
            return NotFound(ApiResponse<AssetCategoryDto>.Fail("Category not found"));
        }

        return Ok(ApiResponse<AssetCategoryDto>.Ok(MapToDto(category)));
    }

    [HttpPost]
    [Authorize(Policy = "CanCreateAssetCategories")]
    public async Task<ActionResult<ApiResponse<AssetCategoryDto>>> CreateCategory(
        [FromBody] CreateAssetCategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue)
        {
            return Unauthorized(ApiResponse<AssetCategoryDto>.Fail("User not authenticated"));
        }

        var category = new AssetCategory
        {
            Name = request.Name,
            Code = request.Code,
            Description = request.Description,
            ParentId = request.ParentId,
            SortOrder = request.SortOrder
        };

        var created = await _categoryService.CreateCategoryAsync(category, userId.Value, cancellationToken);

        return CreatedAtAction(
            nameof(GetCategory),
            new { id = created.Id },
            ApiResponse<AssetCategoryDto>.Ok(MapToDto(created), "Category created successfully"));
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "CanEditAssetCategories")]
    public async Task<ActionResult<ApiResponse<AssetCategoryDto>>> UpdateCategory(
        int id,
        [FromBody] CreateAssetCategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue)
        {
            return Unauthorized(ApiResponse<AssetCategoryDto>.Fail("User not authenticated"));
        }

        var category = await _categoryService.GetCategoryByIdAsync(id, cancellationToken);
        if (category == null)
        {
            return NotFound(ApiResponse<AssetCategoryDto>.Fail("Category not found"));
        }

        category.Name = request.Name;
        category.Code = request.Code;
        category.Description = request.Description;
        category.ParentId = request.ParentId;
        category.SortOrder = request.SortOrder;

        var updated = await _categoryService.UpdateCategoryAsync(category, userId.Value, cancellationToken);

        return Ok(ApiResponse<AssetCategoryDto>.Ok(MapToDto(updated), "Category updated successfully"));
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "CanDeleteAssetCategories")]
    public async Task<ActionResult<ApiResponse>> DeleteCategory(int id, CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue)
        {
            return Unauthorized(ApiResponse.Fail("User not authenticated"));
        }

        var success = await _categoryService.DeleteCategoryAsync(id, userId.Value, cancellationToken);

        if (!success)
        {
            return BadRequest(ApiResponse.Fail("Cannot delete category. It may have assets or not exist."));
        }

        return Ok(ApiResponse.Ok("Category deleted successfully"));
    }

    private static AssetCategoryDto MapToDto(AssetCategory category)
    {
        return new AssetCategoryDto
        {
            Id = category.Id,
            Name = category.Name,
            Code = category.Code,
            Description = category.Description,
            ParentId = category.ParentId,
            ParentName = category.Parent?.Name,
            Level = category.Level,
            SortOrder = category.SortOrder,
            IsActive = category.IsActive,
            Children = category.Children
                .Where(c => !c.IsDeleted)
                .OrderBy(c => c.SortOrder)
                .ThenBy(c => c.Name)
                .Select(MapToDto)
                .ToList()
        };
    }
}
