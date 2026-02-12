using LicensingServer.Web.Data;
using LicensingServer.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace LicensingServer.Web.Pages.Customers;

public class IndexModel : PageModel
{
    private readonly LicensingDbContext _context;

    public IndexModel(LicensingDbContext context)
    {
        _context = context;
    }

    public List<Customer> Customers { get; set; } = new();

    public async Task OnGetAsync()
    {
        Customers = await _context.Customers
            .Include(c => c.Licenses)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var customer = await _context.Customers.FindAsync(id);
        if (customer != null)
        {
            customer.IsActive = false;
            customer.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
        return RedirectToPage();
    }
}
