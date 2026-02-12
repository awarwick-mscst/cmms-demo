using LicensingServer.Web.Data;
using LicensingServer.Web.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace LicensingServer.Web.Pages.Activations;

public class IndexModel : PageModel
{
    private readonly LicensingDbContext _context;

    public IndexModel(LicensingDbContext context)
    {
        _context = context;
    }

    public List<LicenseActivation> Activations { get; set; } = new();

    public async Task OnGetAsync()
    {
        Activations = await _context.Activations
            .Include(a => a.License)
            .ThenInclude(l => l.Customer)
            .OrderByDescending(a => a.ActivatedAt)
            .ToListAsync();
    }
}
