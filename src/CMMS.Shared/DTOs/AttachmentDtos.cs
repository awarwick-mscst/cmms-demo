namespace CMMS.Shared.DTOs;

public class AttachmentDto
{
    public int Id { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public int EntityId { get; set; }
    public string AttachmentType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string MimeType { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsPrimary { get; set; }
    public DateTime UploadedAt { get; set; }
    public int? UploadedBy { get; set; }
    public string? UploadedByName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateAttachmentRequest
{
    public string EntityType { get; set; } = string.Empty;
    public int EntityId { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
}

public class UpdateAttachmentRequest
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public int? DisplayOrder { get; set; }
}

public class AttachmentFilter
{
    public string? EntityType { get; set; }
    public int? EntityId { get; set; }
    public string? AttachmentType { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}
