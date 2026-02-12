using CMMS.API.Attributes;
using CMMS.Core.Entities;
using CMMS.Core.Interfaces;
using CMMS.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CMMS.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
[RequiresFeature("inventory")]
public class SuppliersController : ControllerBase
{
    private readonly ISupplierService _supplierService;
    private readonly ICurrentUserService _currentUserService;

    public SuppliersController(ISupplierService supplierService, ICurrentUserService currentUserService)
    {
        _supplierService = supplierService;
        _currentUserService = currentUserService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResponse<SupplierDto>>> GetSuppliers(
        [FromQuery] string? search,
        [FromQuery] bool? isActive,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? sortBy = null,
        [FromQuery] bool sortDescending = false,
        CancellationToken cancellationToken = default)
    {
        var filter = new SupplierFilter
        {
            Search = search,
            IsActive = isActive,
            Page = page,
            PageSize = Math.Min(pageSize, 100),
            SortBy = sortBy,
            SortDescending = sortDescending
        };

        var result = await _supplierService.GetSuppliersAsync(filter, cancellationToken);

        var response = new PagedResponse<SupplierDto>
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
    public async Task<ActionResult<ApiResponse<SupplierDto>>> GetSupplier(int id, CancellationToken cancellationToken = default)
    {
        var supplier = await _supplierService.GetSupplierByIdAsync(id, cancellationToken);

        if (supplier == null)
            return NotFound(ApiResponse<SupplierDto>.Fail("Supplier not found"));

        return Ok(ApiResponse<SupplierDto>.Ok(MapToDto(supplier)));
    }

    [HttpPost]
    [Authorize(Policy = "CanManageInventory")]
    public async Task<ActionResult<ApiResponse<SupplierDto>>> CreateSupplier(
        [FromBody] CreateSupplierRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue)
            return Unauthorized(ApiResponse<SupplierDto>.Fail("User not authenticated"));

        if (!string.IsNullOrEmpty(request.Code) && await _supplierService.CodeExistsAsync(request.Code, null, cancellationToken))
            return BadRequest(ApiResponse<SupplierDto>.Fail("Supplier code already exists"));

        var supplier = new Supplier
        {
            Name = request.Name,
            Code = request.Code,
            ContactName = request.ContactName,
            Email = request.Email,
            Phone = request.Phone,
            Address = request.Address,
            City = request.City,
            State = request.State,
            PostalCode = request.PostalCode,
            Country = request.Country,
            Website = request.Website,
            Notes = request.Notes,
            IsActive = request.IsActive
        };

        var created = await _supplierService.CreateSupplierAsync(supplier, userId.Value, cancellationToken);
        var result = await _supplierService.GetSupplierByIdAsync(created.Id, cancellationToken);

        return CreatedAtAction(
            nameof(GetSupplier),
            new { id = created.Id },
            ApiResponse<SupplierDto>.Ok(MapToDto(result!), "Supplier created successfully"));
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "CanManageInventory")]
    public async Task<ActionResult<ApiResponse<SupplierDto>>> UpdateSupplier(
        int id,
        [FromBody] UpdateSupplierRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue)
            return Unauthorized(ApiResponse<SupplierDto>.Fail("User not authenticated"));

        var supplier = await _supplierService.GetSupplierByIdAsync(id, cancellationToken);
        if (supplier == null)
            return NotFound(ApiResponse<SupplierDto>.Fail("Supplier not found"));

        if (!string.IsNullOrEmpty(request.Code) && await _supplierService.CodeExistsAsync(request.Code, id, cancellationToken))
            return BadRequest(ApiResponse<SupplierDto>.Fail("Supplier code already exists"));

        supplier.Name = request.Name;
        supplier.Code = request.Code;
        supplier.ContactName = request.ContactName;
        supplier.Email = request.Email;
        supplier.Phone = request.Phone;
        supplier.Address = request.Address;
        supplier.City = request.City;
        supplier.State = request.State;
        supplier.PostalCode = request.PostalCode;
        supplier.Country = request.Country;
        supplier.Website = request.Website;
        supplier.Notes = request.Notes;
        supplier.IsActive = request.IsActive;

        await _supplierService.UpdateSupplierAsync(supplier, userId.Value, cancellationToken);
        var result = await _supplierService.GetSupplierByIdAsync(id, cancellationToken);

        return Ok(ApiResponse<SupplierDto>.Ok(MapToDto(result!), "Supplier updated successfully"));
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "CanManageInventory")]
    public async Task<ActionResult<ApiResponse>> DeleteSupplier(int id, CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue)
            return Unauthorized(ApiResponse.Fail("User not authenticated"));

        var success = await _supplierService.DeleteSupplierAsync(id, userId.Value, cancellationToken);

        if (!success)
            return NotFound(ApiResponse.Fail("Supplier not found"));

        return Ok(ApiResponse.Ok("Supplier deleted successfully"));
    }

    private static SupplierDto MapToDto(Supplier supplier)
    {
        return new SupplierDto
        {
            Id = supplier.Id,
            Name = supplier.Name,
            Code = supplier.Code,
            ContactName = supplier.ContactName,
            Email = supplier.Email,
            Phone = supplier.Phone,
            Address = supplier.Address,
            City = supplier.City,
            State = supplier.State,
            PostalCode = supplier.PostalCode,
            Country = supplier.Country,
            Website = supplier.Website,
            Notes = supplier.Notes,
            IsActive = supplier.IsActive,
            PartCount = supplier.Parts?.Count ?? 0,
            CreatedAt = supplier.CreatedAt,
            UpdatedAt = supplier.UpdatedAt
        };
    }
}
