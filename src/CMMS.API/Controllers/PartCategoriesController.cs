using CMMS.Core.Entities;
using CMMS.Core.Interfaces;
using CMMS.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CMMS.API.Controllers;

[ApiController]
[Route("api/v1/part-categories")]
[Authorize]
public class PartCategoriesController : ControllerBase
{
    private readonly IPartCategoryService _categoryService;
    private readonly ICurrentUserService _currentUserService;

    public PartCategoriesController(IPartCategoryService categoryService, ICurrentUserService currentUserService)
    {
        _categoryService = categoryService;
        _currentUserService = currentUserService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<PartCategoryDto>>>> GetCategories(
        [FromQuery] bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        var categories = await _categoryService.GetCategoryTreeAsync(includeInactive, cancellationToken);
        var dtos = categories.Select(MapToDto);
        return Ok(ApiResponse<IEnumerable<PartCategoryDto>>.Ok(dtos));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<PartCategoryDto>>> GetCategory(int id, CancellationToken cancellationToken = default)
    {
        var category = await _categoryService.GetCategoryByIdAsync(id, cancellationToken);

        if (category == null)
            return NotFound(ApiResponse<PartCategoryDto>.Fail("Category not found"));

        return Ok(ApiResponse<PartCategoryDto>.Ok(MapToDto(category)));
    }

    [HttpPost]
    [Authorize(Policy = "CanManageInventory")]
    public async Task<ActionResult<ApiResponse<PartCategoryDto>>> CreateCategory(
        [FromBody] CreatePartCategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue)
            return Unauthorized(ApiResponse<PartCategoryDto>.Fail("User not authenticated"));

        if (!string.IsNullOrEmpty(request.Code) && await _categoryService.CodeExistsAsync(request.Code, null, cancellationToken))
            return BadRequest(ApiResponse<PartCategoryDto>.Fail("Category code already exists"));

        var category = new PartCategory
        {
            Name = request.Name,
            Code = request.Code,
            Description = request.Description,
            ParentId = request.ParentId,
            SortOrder = request.SortOrder,
            IsActive = request.IsActive
        };

        var created = await _categoryService.CreateCategoryAsync(category, userId.Value, cancellationToken);
        var result = await _categoryService.GetCategoryByIdAsync(created.Id, cancellationToken);

        return CreatedAtAction(
            nameof(GetCategory),
            new { id = created.Id },
            ApiResponse<PartCategoryDto>.Ok(MapToDto(result!), "Category created successfully"));
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "CanManageInventory")]
    public async Task<ActionResult<ApiResponse<PartCategoryDto>>> UpdateCategory(
        int id,
        [FromBody] UpdatePartCategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue)
            return Unauthorized(ApiResponse<PartCategoryDto>.Fail("User not authenticated"));

        var category = await _categoryService.GetCategoryByIdAsync(id, cancellationToken);
        if (category == null)
            return NotFound(ApiResponse<PartCategoryDto>.Fail("Category not found"));

        if (!string.IsNullOrEmpty(request.Code) && await _categoryService.CodeExistsAsync(request.Code, id, cancellationToken))
            return BadRequest(ApiResponse<PartCategoryDto>.Fail("Category code already exists"));

        if (request.ParentId == id)
            return BadRequest(ApiResponse<PartCategoryDto>.Fail("Category cannot be its own parent"));

        category.Name = request.Name;
        category.Code = request.Code;
        category.Description = request.Description;
        category.ParentId = request.ParentId;
        category.SortOrder = request.SortOrder;
        category.IsActive = request.IsActive;

        await _categoryService.UpdateCategoryAsync(category, userId.Value, cancellationToken);
        var result = await _categoryService.GetCategoryByIdAsync(id, cancellationToken);

        return Ok(ApiResponse<PartCategoryDto>.Ok(MapToDto(result!), "Category updated successfully"));
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "CanManageInventory")]
    public async Task<ActionResult<ApiResponse>> DeleteCategory(int id, CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue)
            return Unauthorized(ApiResponse.Fail("User not authenticated"));

        if (await _categoryService.HasPartsAsync(id, cancellationToken))
            return BadRequest(ApiResponse.Fail("Cannot delete category with associated parts"));

        var success = await _categoryService.DeleteCategoryAsync(id, userId.Value, cancellationToken);

        if (!success)
            return NotFound(ApiResponse.Fail("Category not found"));

        return Ok(ApiResponse.Ok("Category deleted successfully"));
    }

    private static PartCategoryDto MapToDto(PartCategory category)
    {
        return new PartCategoryDto
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
            PartCount = category.Parts?.Count ?? 0,
            Children = category.Children?.Select(MapToDto).ToList() ?? new List<PartCategoryDto>(),
            CreatedAt = category.CreatedAt,
            UpdatedAt = category.UpdatedAt
        };
    }
}
