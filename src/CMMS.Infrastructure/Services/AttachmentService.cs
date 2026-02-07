using CMMS.Core.Entities;
using CMMS.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CMMS.Infrastructure.Services;

public class AttachmentService : IAttachmentService
{
    private readonly IUnitOfWork _unitOfWork;

    public AttachmentService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<Attachment>> GetAttachmentsAsync(string entityType, int entityId, CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.Attachments.Query()
            .Include(a => a.Uploader)
            .Where(a => a.EntityType == entityType && a.EntityId == entityId)
            .OrderBy(a => a.DisplayOrder)
            .ThenBy(a => a.UploadedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Attachment?> GetAttachmentByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.Attachments.Query()
            .Include(a => a.Uploader)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public async Task<Attachment> CreateAttachmentAsync(Attachment attachment, int createdBy, CancellationToken cancellationToken = default)
    {
        attachment.CreatedBy = createdBy;
        attachment.CreatedAt = DateTime.UtcNow;
        attachment.UploadedBy = createdBy;
        attachment.UploadedAt = DateTime.UtcNow;

        await _unitOfWork.Attachments.AddAsync(attachment, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return attachment;
    }

    public async Task<Attachment> UpdateAttachmentAsync(Attachment attachment, int updatedBy, CancellationToken cancellationToken = default)
    {
        attachment.UpdatedBy = updatedBy;
        attachment.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Attachments.Update(attachment);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return attachment;
    }

    public async Task<bool> DeleteAttachmentAsync(int id, int deletedBy, CancellationToken cancellationToken = default)
    {
        var attachment = await _unitOfWork.Attachments.GetByIdAsync(id, cancellationToken);
        if (attachment == null)
            return false;

        attachment.IsDeleted = true;
        attachment.DeletedAt = DateTime.UtcNow;
        attachment.UpdatedBy = deletedBy;
        attachment.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<int> GetNextDisplayOrderAsync(string entityType, int entityId, CancellationToken cancellationToken = default)
    {
        var maxOrder = await _unitOfWork.Attachments.Query()
            .Where(a => a.EntityType == entityType && a.EntityId == entityId)
            .MaxAsync(a => (int?)a.DisplayOrder, cancellationToken);

        return (maxOrder ?? 0) + 1;
    }

    public async Task<bool> SetPrimaryImageAsync(int attachmentId, int updatedBy, CancellationToken cancellationToken = default)
    {
        var attachment = await _unitOfWork.Attachments.GetByIdAsync(attachmentId, cancellationToken);
        if (attachment == null || attachment.AttachmentType != "Image")
            return false;

        // Clear any existing primary image for this entity
        var existingPrimary = await _unitOfWork.Attachments.Query()
            .Where(a => a.EntityType == attachment.EntityType
                     && a.EntityId == attachment.EntityId
                     && a.IsPrimary
                     && a.Id != attachmentId)
            .ToListAsync(cancellationToken);

        foreach (var existing in existingPrimary)
        {
            existing.IsPrimary = false;
            existing.UpdatedBy = updatedBy;
            existing.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.Attachments.Update(existing);
        }

        // Set new primary
        attachment.IsPrimary = true;
        attachment.UpdatedBy = updatedBy;
        attachment.UpdatedAt = DateTime.UtcNow;

        // Explicitly mark as modified to ensure EF tracks the change
        _unitOfWork.Attachments.Update(attachment);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<Attachment?> GetPrimaryImageAsync(string entityType, int entityId, CancellationToken cancellationToken = default)
    {
        // First try to get the primary image
        var primary = await _unitOfWork.Attachments.Query()
            .Include(a => a.Uploader)
            .Where(a => a.EntityType == entityType
                     && a.EntityId == entityId
                     && a.AttachmentType == "Image"
                     && a.IsPrimary)
            .FirstOrDefaultAsync(cancellationToken);

        if (primary != null)
            return primary;

        // If no primary is set, return the first image by display order
        return await _unitOfWork.Attachments.Query()
            .Include(a => a.Uploader)
            .Where(a => a.EntityType == entityType
                     && a.EntityId == entityId
                     && a.AttachmentType == "Image")
            .OrderBy(a => a.DisplayOrder)
            .ThenBy(a => a.UploadedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
