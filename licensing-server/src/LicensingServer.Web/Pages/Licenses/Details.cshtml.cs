using LicensingServer.Web.Data;
using LicensingServer.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace LicensingServer.Web.Pages.Licenses;

public class DetailsModel : PageModel
{
    private readonly LicensingDbContext _context;

    public DetailsModel(LicensingDbContext context)
    {
        _context = context;
    }

    public License License { get; set; } = null!;
    public List<LicenseAuditLog> AuditLogs { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var license = await _context.Licenses
            .Include(l => l.Customer)
            .Include(l => l.Activations)
            .FirstOrDefaultAsync(l => l.Id == id);

        if (license == null) return NotFound();

        License = license;
        AuditLogs = await _context.AuditLogs
            .Where(a => a.LicenseId == id)
            .OrderByDescending(a => a.Timestamp)
            .Take(20)
            .ToListAsync();

        return Page();
    }
}
