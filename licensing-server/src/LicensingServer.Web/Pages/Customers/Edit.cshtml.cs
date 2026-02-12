using LicensingServer.Web.Data;
using LicensingServer.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LicensingServer.Web.Pages.Customers;

public class EditModel : PageModel
{
    private readonly LicensingDbContext _context;

    public EditModel(LicensingDbContext context)
    {
        _context = context;
    }

    [BindProperty]
    public CustomerFormViewModel Form { get; set; } = new();

    public int CustomerId { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var customer = await _context.Customers.FindAsync(id);
        if (customer == null) return NotFound();

        CustomerId = id;
        Form = new CustomerFormViewModel
        {
            CompanyName = customer.CompanyName,
            ContactName = customer.ContactName,
            ContactEmail = customer.ContactEmail,
            Phone = customer.Phone,
            Notes = customer.Notes,
        };

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        if (!ModelState.IsValid)
        {
            CustomerId = id;
            return Page();
        }

        var customer = await _context.Customers.FindAsync(id);
        if (customer == null) return NotFound();

        customer.CompanyName = Form.CompanyName;
        customer.ContactName = Form.ContactName;
        customer.ContactEmail = Form.ContactEmail;
        customer.Phone = Form.Phone;
        customer.Notes = Form.Notes;
        customer.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return RedirectToPage("Index");
    }
}
