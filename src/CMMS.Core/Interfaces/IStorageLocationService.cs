using CMMS.Core.Entities;

namespace CMMS.Core.Interfaces;

public interface IStorageLocationService
{
    Task<IEnumerable<StorageLocation>> GetLocationsAsync(bool includeInactive = false, CancellationToken cancellationToken = default);
    Task<IEnumerable<StorageLocation>> GetLocationTreeAsync(bool includeInactive = false, CancellationToken cancellationToken = default);
    Task<StorageLocation?> GetLocationByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<StorageLocation?> GetLocationByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<StorageLocation> CreateLocationAsync(StorageLocation location, int createdBy, CancellationToken cancellationToken = default);
    Task<StorageLocation> UpdateLocationAsync(StorageLocation location, int updatedBy, CancellationToken cancellationToken = default);
    Task<bool> DeleteLocationAsync(int id, int deletedBy, CancellationToken cancellationToken = default);
    Task<bool> CodeExistsAsync(string code, int? excludeId = null, CancellationToken cancellationToken = default);
    Task<bool> HasStockAsync(int locationId, CancellationToken cancellationToken = default);
    Task<string> BuildFullPathAsync(int? parentId, string name, CancellationToken cancellationToken = default);
}
