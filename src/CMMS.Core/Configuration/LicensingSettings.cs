namespace CMMS.Core.Configuration;

public class LicensingSettings
{
    public const string SectionName = "Licensing";

    public bool Enabled { get; set; } = true;
    public string LicenseServerUrl { get; set; } = "http://localhost:5100";
    public string PublicKeyPath { get; set; } = string.Empty;
    public string? PublicKeyPem { get; set; }
    public int GracePeriodDays { get; set; } = 30;
    public int PhoneHomeIntervalHours { get; set; } = 24;
    public int WarningDaysBeforeExpiry { get; set; } = 14;
}
