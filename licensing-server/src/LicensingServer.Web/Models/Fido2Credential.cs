namespace LicensingServer.Web.Models;

public class Fido2Credential
{
    public int Id { get; set; }
    public int AdminUserId { get; set; }
    public byte[] CredentialId { get; set; } = Array.Empty<byte>();
    public byte[] PublicKey { get; set; } = Array.Empty<byte>();
    public long SignatureCounter { get; set; }
    public Guid? AaGuid { get; set; }
    public string DeviceName { get; set; } = string.Empty;
    public string CredentialType { get; set; } = "public-key";
    public string? Transports { get; set; }
    public bool IsBackupEligible { get; set; }
    public bool IsBackupDevice { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
    public DateTime? RevokedAt { get; set; }

    public virtual AdminUser AdminUser { get; set; } = null!;
}
