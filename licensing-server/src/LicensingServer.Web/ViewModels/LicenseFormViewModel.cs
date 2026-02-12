using System.ComponentModel.DataAnnotations;

namespace LicensingServer.Web.ViewModels;

public class LicenseFormViewModel
{
    [Required]
    [Display(Name = "Customer")]
    public int CustomerId { get; set; }

    [Required]
    [Display(Name = "Tier")]
    public string Tier { get; set; } = "Basic";

    [Required]
    [Display(Name = "Max Activations")]
    [Range(1, 100)]
    public int MaxActivations { get; set; } = 1;

    [Required]
    [Display(Name = "Duration (Months)")]
    [Range(1, 60)]
    public int DurationMonths { get; set; } = 12;

    [Display(Name = "Notes")]
    public string? Notes { get; set; }
}

public class CustomerFormViewModel
{
    [Required]
    [Display(Name = "Company Name")]
    [MaxLength(200)]
    public string CompanyName { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Contact Name")]
    [MaxLength(200)]
    public string ContactName { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Contact Email")]
    [EmailAddress]
    [MaxLength(200)]
    public string ContactEmail { get; set; } = string.Empty;

    [Display(Name = "Phone")]
    [MaxLength(50)]
    public string? Phone { get; set; }

    [Display(Name = "Notes")]
    [MaxLength(2000)]
    public string? Notes { get; set; }
}
