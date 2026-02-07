using CMMS.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace CMMS.Infrastructure.Services;

public class FileStorageService : IFileStorageService
{
    private readonly string _webRootPath;
    private readonly ILogger<FileStorageService> _logger;

    private const long MaxFileSize = 10 * 1024 * 1024; // 10 MB
    private const string UploadsFolder = "uploads";

    private static readonly HashSet<string> AllowedImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".gif", ".webp"
    };

    private static readonly HashSet<string> AllowedDocumentExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".txt"
    };

    private static readonly Dictionary<string, string> MimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        { ".jpg", "image/jpeg" },
        { ".jpeg", "image/jpeg" },
        { ".png", "image/png" },
        { ".gif", "image/gif" },
        { ".webp", "image/webp" },
        { ".pdf", "application/pdf" },
        { ".doc", "application/msword" },
        { ".docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document" },
        { ".xls", "application/vnd.ms-excel" },
        { ".xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" },
        { ".txt", "text/plain" }
    };

    public FileStorageService(string webRootPath, ILogger<FileStorageService> logger)
    {
        _webRootPath = webRootPath;
        _logger = logger;
    }

    public async Task<FileUploadResult> SaveFileAsync(Stream fileStream, string fileName, string entityType, int entityId, CancellationToken cancellationToken = default)
    {
        try
        {
            var extension = Path.GetExtension(fileName);
            var mimeType = GetMimeType(fileName);
            var attachmentType = GetAttachmentType(mimeType);

            // Generate unique filename
            var uniqueFileName = $"{Guid.NewGuid()}-{SanitizeFileName(fileName)}";

            // Build relative path: uploads/{entityType}/{entityId}/{uniqueFileName}
            var entityFolder = GetEntityFolder(entityType);
            var relativePath = Path.Combine(UploadsFolder, entityFolder, entityId.ToString(), uniqueFileName);

            // Build full path
            var fullPath = Path.Combine(_webRootPath, relativePath);
            var directory = Path.GetDirectoryName(fullPath);

            if (directory != null && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Save file
            using (var fs = new FileStream(fullPath, FileMode.Create))
            {
                await fileStream.CopyToAsync(fs, cancellationToken);
            }

            var fileInfo = new FileInfo(fullPath);

            _logger.LogInformation("File saved: {FilePath}, Size: {FileSize}", relativePath, fileInfo.Length);

            return FileUploadResult.Ok(
                relativePath.Replace('\\', '/'),
                fileName,
                fileInfo.Length,
                mimeType,
                attachmentType
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving file: {FileName}", fileName);
            return FileUploadResult.Fail($"Error saving file: {ex.Message}");
        }
    }

    public Task<bool> DeleteFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        try
        {
            var fullPath = Path.Combine(_webRootPath, filePath.Replace('/', Path.DirectorySeparatorChar));

            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
                _logger.LogInformation("File deleted: {FilePath}", filePath);
                return Task.FromResult(true);
            }

            _logger.LogWarning("File not found for deletion: {FilePath}", filePath);
            return Task.FromResult(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file: {FilePath}", filePath);
            return Task.FromResult(false);
        }
    }

    public bool ValidateFile(string fileName, long fileSize, out string? errorMessage)
    {
        errorMessage = null;

        // Check file size
        if (fileSize > MaxFileSize)
        {
            errorMessage = $"File size exceeds the maximum allowed size of {MaxFileSize / (1024 * 1024)} MB.";
            return false;
        }

        if (fileSize == 0)
        {
            errorMessage = "File is empty.";
            return false;
        }

        // Check file extension
        var extension = Path.GetExtension(fileName);
        if (string.IsNullOrEmpty(extension))
        {
            errorMessage = "File must have an extension.";
            return false;
        }

        if (!AllowedImageExtensions.Contains(extension) && !AllowedDocumentExtensions.Contains(extension))
        {
            errorMessage = $"File type '{extension}' is not allowed. Allowed types: {string.Join(", ", AllowedImageExtensions.Concat(AllowedDocumentExtensions))}";
            return false;
        }

        return true;
    }

    public string GetFileUrl(string filePath)
    {
        // Return relative URL for use with static file middleware
        return "/" + filePath.Replace('\\', '/');
    }

    public string GetMimeType(string fileName)
    {
        var extension = Path.GetExtension(fileName);
        return MimeTypes.TryGetValue(extension, out var mimeType)
            ? mimeType
            : "application/octet-stream";
    }

    public string GetAttachmentType(string mimeType)
    {
        return mimeType.StartsWith("image/") ? "Image" : "Document";
    }

    private static string GetEntityFolder(string entityType)
    {
        return entityType.ToLower() switch
        {
            "asset" => "assets",
            "part" => "parts",
            "workorder" => "workorders",
            _ => "other"
        };
    }

    private static string SanitizeFileName(string fileName)
    {
        // Remove invalid path characters and limit length
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = new string(fileName.Where(c => !invalidChars.Contains(c)).ToArray());

        // Limit filename length
        if (sanitized.Length > 100)
        {
            var extension = Path.GetExtension(sanitized);
            var nameWithoutExt = Path.GetFileNameWithoutExtension(sanitized);
            sanitized = nameWithoutExt.Substring(0, 100 - extension.Length) + extension;
        }

        return sanitized;
    }
}
