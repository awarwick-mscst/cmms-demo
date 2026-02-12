namespace LicensingServer.Web.ViewModels;

public class DashboardViewModel
{
    public int TotalCustomers { get; set; }
    public int ActiveLicenses { get; set; }
    public int ActiveActivations { get; set; }
    public int ExpiringSoon { get; set; }
    public List<RecentActivityItem> RecentActivity { get; set; } = new();
    public Dictionary<string, int> LicensesByTier { get; set; } = new();
}

public class RecentActivityItem
{
    public string Action { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}
