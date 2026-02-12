using CMMS.Core.Enums;

namespace CMMS.Core.Licensing;

public static class FeatureGate
{
    // Feature key constants
    public const string WorkOrders = "work-orders";
    public const string Assets = "assets";
    public const string Inventory = "inventory";
    public const string PreventiveMaintenance = "preventive-maintenance";
    public const string AdvancedReporting = "advanced-reporting";
    public const string LabelPrinting = "label-printing";
    public const string Ldap = "ldap";
    public const string EmailCalendar = "email-calendar";
    public const string Backup = "backup";
    public const string ApiAccess = "api-access";

    private static readonly Dictionary<LicenseTier, HashSet<string>> TierFeatures = new()
    {
        [LicenseTier.Basic] = new HashSet<string>
        {
            WorkOrders,
            Assets,
        },
        [LicenseTier.Pro] = new HashSet<string>
        {
            WorkOrders,
            Assets,
            Inventory,
            PreventiveMaintenance,
            AdvancedReporting,
            LabelPrinting,
        },
        [LicenseTier.Enterprise] = new HashSet<string>
        {
            WorkOrders,
            Assets,
            Inventory,
            PreventiveMaintenance,
            AdvancedReporting,
            LabelPrinting,
            Ldap,
            EmailCalendar,
            Backup,
            ApiAccess,
        },
    };

    public static HashSet<string> GetFeaturesForTier(LicenseTier tier)
    {
        return TierFeatures.TryGetValue(tier, out var features) ? features : TierFeatures[LicenseTier.Basic];
    }

    public static bool IsTierFeature(LicenseTier tier, string feature)
    {
        return GetFeaturesForTier(tier).Contains(feature);
    }

    public static string[] GetAllFeatures()
    {
        return new[]
        {
            WorkOrders, Assets, Inventory, PreventiveMaintenance,
            AdvancedReporting, LabelPrinting, Ldap, EmailCalendar,
            Backup, ApiAccess,
        };
    }
}
