using LicensingServer.Web.Data;
using LicensingServer.Web.Models;
using LicensingServer.Web.Services;
using LicensingServer.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace LicensingServer.Web.Pages.Licenses;

public class CreateModel : PageModel
{
    private readonly LicensingDbContext _context;
    private readonly LicenseKeyGenerator _keyGenerator;

    public CreateModel(LicensingDbContext context, LicenseKeyGenerator keyGenerator)
    {
        _context = context;
        _keyGenerator = keyGenerator;
    }

    [BindProperty]
    public LicenseFormViewModel Form { get; set; } = new();

    public List<SelectListItem> Customers { get; set; } = new();
    public string[] Tiers { get; set; } = { "Basic", "Pro", "Enterprise" };

    public async Task OnGetAsync()
    {
        await LoadCustomers();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await LoadCustomers();
            return Page();
        }

        var expiresAt = DateTime.UtcNow.AddMonths(Form.DurationMonths);

        var payload = new LicensePayload
        {
            CustomerId = Form.CustomerId,
            Tier = Form.Tier,
            MaxActivations = Form.MaxActivations,
            IssuedAt = DateTime.UtcNow,
            ExpiresAt = expiresAt,
            Features = ActivationResult.GetFeaturesForTier(Form.Tier),
        };

        var license = new License
        {
            CustomerId = Form.CustomerId,
            Tier = Form.Tier,
            MaxActivations = Form.MaxActivations,
            ExpiresAt = expiresAt,
            Notes = Form.Notes,
        };

        // Generate the signed key (we need the ID first, so save, generate, update)
        _context.Licenses.Add(license);
        await _context.SaveChangesAsync();

        payload.LicenseId = license.Id;
        license.LicenseKey = _keyGenerator.GenerateLicenseKey(payload);
        license.UpdatedAt = DateTime.UtcNow;

        _context.AuditLogs.Add(new LicenseAuditLog
        {
            LicenseId = license.Id,
            Action = "Created",
            Details = $"Tier: {license.Tier}, Expires: {license.ExpiresAt:yyyy-MM-dd}",
        });

        await _context.SaveChangesAsync();

        return RedirectToPage("Details", new { id = license.Id });
    }

    private async Task LoadCustomers()
    {
        Customers = await _context.Customers
            .Where(c => c.IsActive)
            .OrderBy(c => c.CompanyName)
            .Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.CompanyName })
            .ToListAsync();
    }
}
