using LicensingServer.Web.Data;
using LicensingServer.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace LicensingServer.Web.Pages.Licenses;

public class IndexModel : PageModel
{
    private readonly LicensingDbContext _context;

    public IndexModel(LicensingDbContext context)
    {
        _context = context;
    }

    public List<License> Licenses { get; set; } = new();

    public async Task OnGetAsync()
    {
        Licenses = await _context.Licenses
            .Include(l => l.Customer)
            .Include(l => l.Activations.Where(a => a.IsActive))
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostRevokeAsync(int id, string reason)
    {
        var license = await _context.Licenses.FindAsync(id);
        if (license != null)
        {
            license.IsRevoked = true;
            license.RevokedAt = DateTime.UtcNow;
            license.RevokedReason = reason;
            license.UpdatedAt = DateTime.UtcNow;

            _context.AuditLogs.Add(new LicenseAuditLog
            {
                LicenseId = id,
                Action = "Revoked",
                Details = reason,
            });

            await _context.SaveChangesAsync();
        }
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostExtendAsync(int id, int months)
    {
        var license = await _context.Licenses.FindAsync(id);
        if (license != null)
        {
            license.ExpiresAt = license.ExpiresAt.AddMonths(months);
            license.UpdatedAt = DateTime.UtcNow;

            _context.AuditLogs.Add(new LicenseAuditLog
            {
                LicenseId = id,
                Action = "Extended",
                Details = $"Extended by {months} months. New expiry: {license.ExpiresAt:yyyy-MM-dd}",
            });

            await _context.SaveChangesAsync();
        }
        return RedirectToPage();
    }
}
