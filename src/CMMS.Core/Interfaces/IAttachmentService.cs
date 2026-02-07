using CMMS.Core.Entities;

namespace CMMS.Core.Interfaces;

public interface IAttachmentService
{
    Task<IEnumerable<Attachment>> GetAttachmentsAsync(string entityType, int entityId, CancellationToken cancellationToken = default);
    Task<Attachment?> GetAttachmentByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Attachment> CreateAttachmentAsync(Attachment attachment, int createdBy, CancellationToken cancellationToken = default);
    Task<Attachment> UpdateAttachmentAsync(Attachment attachment, int updatedBy, CancellationToken cancellationToken = default);
    Task<bool> DeleteAttachmentAsync(int id, int deletedBy, CancellationToken cancellationToken = default);
    Task<int> GetNextDisplayOrderAsync(string entityType, int entityId, CancellationToken cancellationToken = default);
    Task<bool> SetPrimaryImageAsync(int attachmentId, int updatedBy, CancellationToken cancellationToken = default);
    Task<Attachment?> GetPrimaryImageAsync(string entityType, int entityId, CancellationToken cancellationToken = default);
}
