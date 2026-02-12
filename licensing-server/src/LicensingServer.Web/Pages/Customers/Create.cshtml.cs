using LicensingServer.Web.Data;
using LicensingServer.Web.Models;
using LicensingServer.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LicensingServer.Web.Pages.Customers;

public class CreateModel : PageModel
{
    private readonly LicensingDbContext _context;

    public CreateModel(LicensingDbContext context)
    {
        _context = context;
    }

    [BindProperty]
    public CustomerFormViewModel Form { get; set; } = new();

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        var customer = new Customer
        {
            CompanyName = Form.CompanyName,
            ContactName = Form.ContactName,
            ContactEmail = Form.ContactEmail,
            Phone = Form.Phone,
            Notes = Form.Notes,
        };

        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();

        return RedirectToPage("Index");
    }
}
