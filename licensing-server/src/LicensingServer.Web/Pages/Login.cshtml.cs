using LicensingServer.Web.Data;
using LicensingServer.Web.Services.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace LicensingServer.Web.Pages;

public class LoginModel : PageModel
{
    private readonly LicensingDbContext _db;
    private readonly PasswordHashingService _passwordHasher;
    private readonly AdminAuditService _auditService;
    private readonly PartialAuthCookie _partialAuth;
    private readonly ILogger<LoginModel> _logger;

    private const int MaxFailedAttempts = 5;
    private const int LockoutMinutes = 30;

    public LoginModel(
        LicensingDbContext db,
        PasswordHashingService passwordHasher,
        AdminAuditService auditService,
        PartialAuthCookie partialAuth,
        ILogger<LoginModel> logger)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _auditService = auditService;
        _partialAuth = partialAuth;
        _logger = logger;
    }

    [BindProperty] public string Username { get; set; } = string.Empty;
    [BindProperty] public string Password { get; set; } = string.Empty;

    public string? ErrorMessage { get; set; }
    public string? ReturnUrl { get; set; }

    public async Task<IActionResult> OnGetAsync(string? returnUrl = null)
    {
        if (!await _db.AdminUsers.AnyAsync())
            return RedirectToPage("/Setup/InitialSetup");

        ReturnUrl = returnUrl;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        ReturnUrl = returnUrl;

        if (!await _db.AdminUsers.AnyAsync())
            return RedirectToPage("/Setup/InitialSetup");

        var user = await _db.AdminUsers
            .FirstOrDefaultAsync(u => u.Username == Username);

        if (user == null)
        {
            await _auditService.LogAsync(null, Username, false, "Password", "User not found", HttpContext);
            ErrorMessage = "Invalid username or password.";
            return Page();
        }

        // Check lockout
        if (user.AccountLocked && user.LockedUntil.HasValue && user.LockedUntil > DateTime.UtcNow)
        {
            var remaining = (int)(user.LockedUntil.Value - DateTime.UtcNow).TotalMinutes;
            await _auditService.LogAsync(user.Id, Username, false, "Password", "Account locked", HttpContext);
            ErrorMessage = $"Account locked. Try again in {remaining + 1} minutes.";
            return Page();
        }

        // Unlock if lockout expired
        if (user.AccountLocked && user.LockedUntil.HasValue && user.LockedUntil <= DateTime.UtcNow)
        {
            user.AccountLocked = false;
            user.FailedLoginAttempts = 0;
            user.LockedUntil = null;
        }

        // Verify password
        if (!_passwordHasher.VerifyPassword(Password, user.PasswordHash))
        {
            user.FailedLoginAttempts++;
            user.UpdatedAt = DateTime.UtcNow;

            if (user.FailedLoginAttempts >= MaxFailedAttempts)
            {
                user.AccountLocked = true;
                user.LockedUntil = DateTime.UtcNow.AddMinutes(LockoutMinutes);
                await _db.SaveChangesAsync();
                await _auditService.LogAsync(user.Id, Username, false, "Password", $"Account locked after {MaxFailedAttempts} failed attempts", HttpContext);
                ErrorMessage = $"Account locked for {LockoutMinutes} minutes due to too many failed attempts.";
                return Page();
            }

            await _db.SaveChangesAsync();
            await _auditService.LogAsync(user.Id, Username, false, "Password", "Invalid password", HttpContext);
            ErrorMessage = "Invalid username or password.";
            return Page();
        }

        // Password correct — reset failed attempts
        user.FailedLoginAttempts = 0;
        user.AccountLocked = false;
        user.LockedUntil = null;
        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        await _auditService.LogAsync(user.Id, Username, true, "Password", null, HttpContext);

        // Pass encrypted token via query string (cookies don't survive redirects reliably)
        var token = _partialAuth.CreateToken(user.Id, user.Username, returnUrl ?? "/Dashboard");
        _logger.LogInformation("Login: password verified, redirecting to LoginMfa with token (length={Length})", token.Length);
        return RedirectToPage("/LoginMfa", new { t = token });
    }
}
