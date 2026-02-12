using LicensingServer.Web.Data;
using LicensingServer.Web.ViewModels;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace LicensingServer.Web.Pages.Dashboard;

public class IndexModel : PageModel
{
    private readonly LicensingDbContext _context;

    public IndexModel(LicensingDbContext context)
    {
        _context = context;
    }

    public DashboardViewModel Dashboard { get; set; } = new();

    public async Task OnGetAsync()
    {
        Dashboard.TotalCustomers = await _context.Customers.CountAsync(c => c.IsActive);
        Dashboard.ActiveLicenses = await _context.Licenses.CountAsync(l => !l.IsRevoked && l.ExpiresAt > DateTime.UtcNow);
        Dashboard.ActiveActivations = await _context.Activations.CountAsync(a => a.IsActive);
        Dashboard.ExpiringSoon = await _context.Licenses.CountAsync(l =>
            !l.IsRevoked && l.ExpiresAt > DateTime.UtcNow && l.ExpiresAt <= DateTime.UtcNow.AddDays(30));

        Dashboard.LicensesByTier = await _context.Licenses
            .Where(l => !l.IsRevoked && l.ExpiresAt > DateTime.UtcNow)
            .GroupBy(l => l.Tier)
            .Select(g => new { Tier = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Tier, x => x.Count);

        Dashboard.RecentActivity = await _context.AuditLogs
            .OrderByDescending(a => a.Timestamp)
            .Take(10)
            .Select(a => new RecentActivityItem
            {
                Action = a.Action,
                Details = a.Details ?? "",
                Timestamp = a.Timestamp,
            })
            .ToListAsync();
    }
}
