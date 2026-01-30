using CMMS.Core.Entities;

namespace CMMS.Core.Interfaces;

public interface IPartCategoryService
{
    Task<IEnumerable<PartCategory>> GetCategoriesAsync(bool includeInactive = false, CancellationToken cancellationToken = default);
    Task<IEnumerable<PartCategory>> GetCategoryTreeAsync(bool includeInactive = false, CancellationToken cancellationToken = default);
    Task<PartCategory?> GetCategoryByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<PartCategory?> GetCategoryByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<PartCategory> CreateCategoryAsync(PartCategory category, int createdBy, CancellationToken cancellationToken = default);
    Task<PartCategory> UpdateCategoryAsync(PartCategory category, int updatedBy, CancellationToken cancellationToken = default);
    Task<bool> DeleteCategoryAsync(int id, int deletedBy, CancellationToken cancellationToken = default);
    Task<bool> CodeExistsAsync(string code, int? excludeId = null, CancellationToken cancellationToken = default);
    Task<bool> HasPartsAsync(int categoryId, CancellationToken cancellationToken = default);
}
