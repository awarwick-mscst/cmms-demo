namespace LicensingServer.Web.Models;

public class Release
{
    public int Id { get; set; }
    public string Version { get; set; } = string.Empty;
    public string? ReleaseNotes { get; set; }
    public string FileName { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string Sha256Hash { get; set; } = string.Empty;
    public bool IsRequired { get; set; }
    public bool IsActive { get; set; }
    public string Channel { get; set; } = "stable";
    public DateTime PublishedAt { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
