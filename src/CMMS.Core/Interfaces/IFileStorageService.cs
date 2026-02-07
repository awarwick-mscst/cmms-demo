namespace CMMS.Core.Interfaces;

public interface IFileStorageService
{
    Task<FileUploadResult> SaveFileAsync(Stream fileStream, string fileName, string entityType, int entityId, CancellationToken cancellationToken = default);
    Task<bool> DeleteFileAsync(string filePath, CancellationToken cancellationToken = default);
    bool ValidateFile(string fileName, long fileSize, out string? errorMessage);
    string GetFileUrl(string filePath);
    string GetMimeType(string fileName);
    string GetAttachmentType(string mimeType);
}

public class FileUploadResult
{
    public bool Success { get; set; }
    public string? FilePath { get; set; }
    public string? FileName { get; set; }
    public long FileSize { get; set; }
    public string? MimeType { get; set; }
    public string? AttachmentType { get; set; }
    public string? ErrorMessage { get; set; }

    public static FileUploadResult Ok(string filePath, string fileName, long fileSize, string mimeType, string attachmentType)
    {
        return new FileUploadResult
        {
            Success = true,
            FilePath = filePath,
            FileName = fileName,
            FileSize = fileSize,
            MimeType = mimeType,
            AttachmentType = attachmentType
        };
    }

    public static FileUploadResult Fail(string errorMessage)
    {
        return new FileUploadResult
        {
            Success = false,
            ErrorMessage = errorMessage
        };
    }
}
