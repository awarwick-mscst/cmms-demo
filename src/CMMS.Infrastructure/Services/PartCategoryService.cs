using CMMS.Core.Entities;
using CMMS.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CMMS.Infrastructure.Services;

public class PartCategoryService : IPartCategoryService
{
    private readonly IUnitOfWork _unitOfWork;

    public PartCategoryService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<PartCategory>> GetCategoriesAsync(bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        var query = _unitOfWork.PartCategories.Query()
            .Include(c => c.Parts)
            .AsQueryable();

        if (!includeInactive)
        {
            query = query.Where(c => c.IsActive);
        }

        return await query
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<PartCategory>> GetCategoryTreeAsync(bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        var query = _unitOfWork.PartCategories.Query()
            .Include(c => c.Children)
            .Include(c => c.Parts)
            .AsQueryable();

        if (!includeInactive)
        {
            query = query.Where(c => c.IsActive);
        }

        var categories = await query.ToListAsync(cancellationToken);

        return categories
            .Where(c => c.ParentId == null)
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .ToList();
    }

    public async Task<PartCategory?> GetCategoryByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.PartCategories.Query()
            .Include(c => c.Parent)
            .Include(c => c.Children)
            .Include(c => c.Parts)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<PartCategory?> GetCategoryByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.PartCategories.Query()
            .FirstOrDefaultAsync(c => c.Code == code, cancellationToken);
    }

    public async Task<PartCategory> CreateCategoryAsync(PartCategory category, int createdBy, CancellationToken cancellationToken = default)
    {
        category.CreatedBy = createdBy;
        category.CreatedAt = DateTime.UtcNow;

        if (category.ParentId.HasValue)
        {
            var parent = await _unitOfWork.PartCategories.GetByIdAsync(category.ParentId.Value, cancellationToken);
            category.Level = (parent?.Level ?? 0) + 1;
        }
        else
        {
            category.Level = 0;
        }

        await _unitOfWork.PartCategories.AddAsync(category, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return category;
    }

    public async Task<PartCategory> UpdateCategoryAsync(PartCategory category, int updatedBy, CancellationToken cancellationToken = default)
    {
        category.UpdatedBy = updatedBy;
        category.UpdatedAt = DateTime.UtcNow;

        if (category.ParentId.HasValue)
        {
            var parent = await _unitOfWork.PartCategories.GetByIdAsync(category.ParentId.Value, cancellationToken);
            category.Level = (parent?.Level ?? 0) + 1;
        }
        else
        {
            category.Level = 0;
        }

        _unitOfWork.PartCategories.Update(category);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return category;
    }

    public async Task<bool> DeleteCategoryAsync(int id, int deletedBy, CancellationToken cancellationToken = default)
    {
        var category = await _unitOfWork.PartCategories.GetByIdAsync(id, cancellationToken);
        if (category == null)
            return false;

        category.IsDeleted = true;
        category.DeletedAt = DateTime.UtcNow;
        category.UpdatedBy = deletedBy;
        category.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> CodeExistsAsync(string code, int? excludeId = null, CancellationToken cancellationToken = default)
    {
        var query = _unitOfWork.PartCategories.Query()
            .Where(c => c.Code == code);

        if (excludeId.HasValue)
        {
            query = query.Where(c => c.Id != excludeId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    public async Task<bool> HasPartsAsync(int categoryId, CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.Parts.Query()
            .AnyAsync(p => p.CategoryId == categoryId, cancellationToken);
    }
}
