using System.Security.Claims;
using System.Text.Json;
using Fido2NetLib;
using LicensingServer.Web.Data;
using LicensingServer.Web.Services.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace LicensingServer.Web.Pages;

[AllowAnonymous]
public class LoginMfaModel : PageModel
{
    private readonly LicensingDbContext _db;
    private readonly TotpService _totpService;
    private readonly Fido2AuthService _fido2Service;
    private readonly AdminAuditService _auditService;
    private readonly PartialAuthCookie _partialAuth;
    private readonly ILogger<LoginMfaModel> _logger;

    public LoginMfaModel(
        LicensingDbContext db,
        TotpService totpService,
        Fido2AuthService fido2Service,
        AdminAuditService auditService,
        PartialAuthCookie partialAuth,
        ILogger<LoginMfaModel> logger)
    {
        _db = db;
        _totpService = totpService;
        _fido2Service = fido2Service;
        _auditService = auditService;
        _partialAuth = partialAuth;
        _logger = logger;
    }

    [BindProperty] public string TotpCode { get; set; } = string.Empty;
    [BindProperty] public bool UseRecoveryCode { get; set; }

    public string? ErrorMessage { get; set; }
    public bool HasFido2 { get; set; }
    public bool HasTotp { get; set; }
    public string Username { get; set; } = string.Empty;

    public async Task<IActionResult> OnGetAsync()
    {
        _logger.LogInformation("LoginMfa.OnGetAsync called. Path={Path}, QueryString={QS}",
            HttpContext.Request.Path, HttpContext.Request.QueryString);

        var authData = _partialAuth.Get(HttpContext);
        if (authData == null)
        {
            _logger.LogWarning("LoginMfa.OnGetAsync: no auth data found, redirecting to Login");
            return RedirectToPage("/Login");
        }

        _logger.LogInformation("LoginMfa.OnGetAsync: auth data found for userId={UserId}", authData.UserId);

        var user = await _db.AdminUsers
            .Include(u => u.Fido2Credentials)
            .FirstOrDefaultAsync(u => u.Id == authData.UserId);

        if (user == null) return RedirectToPage("/Login");

        Username = user.Username;
        HasTotp = user.TotpEnabled;
        HasFido2 = user.Fido2Credentials.Any(c => c.RevokedAt == null);

        return Page();
    }

    // TOTP verification
    public async Task<IActionResult> OnPostVerifyTotpAsync()
    {
        var authData = _partialAuth.Get(HttpContext);
        if (authData == null) return RedirectToPage("/Login");

        var user = await _db.AdminUsers
            .Include(u => u.Fido2Credentials)
            .FirstOrDefaultAsync(u => u.Id == authData.UserId);

        if (user == null) return RedirectToPage("/Login");

        Username = user.Username;
        HasTotp = user.TotpEnabled;
        HasFido2 = user.Fido2Credentials.Any(c => c.RevokedAt == null);

        if (UseRecoveryCode)
        {
            if (string.IsNullOrEmpty(user.RecoveryCodesEncrypted))
            {
                ErrorMessage = "No recovery codes available.";
                return Page();
            }

            var codes = JsonSerializer.Deserialize<List<string>>(user.RecoveryCodesEncrypted) ?? new();
            if (_totpService.VerifyRecoveryCode(TotpCode, codes))
            {
                codes.Remove(TotpCode.Trim().ToLowerInvariant());
                user.RecoveryCodesEncrypted = JsonSerializer.Serialize(codes);
                await _db.SaveChangesAsync();

                await _auditService.LogAsync(user.Id, user.Username, true, "RecoveryCode", null, HttpContext);
                return await CompleteLoginAsync(user, authData.ReturnUrl);
            }

            await _auditService.LogAsync(user.Id, user.Username, false, "RecoveryCode", "Invalid recovery code", HttpContext);
            ErrorMessage = "Invalid recovery code.";
            return Page();
        }

        // Verify TOTP code
        if (string.IsNullOrEmpty(user.TotpSecretEncrypted))
        {
            ErrorMessage = "TOTP is not configured.";
            return Page();
        }

        if (_totpService.VerifyCode(user.TotpSecretEncrypted, TotpCode))
        {
            await _auditService.LogAsync(user.Id, user.Username, true, "TOTP", null, HttpContext);
            return await CompleteLoginAsync(user, authData.ReturnUrl);
        }

        await _auditService.LogAsync(user.Id, user.Username, false, "TOTP", "Invalid code", HttpContext);
        ErrorMessage = "Invalid authentication code. Please try again.";
        return Page();
    }

    // FIDO2 authentication - get options (AJAX)
    public async Task<IActionResult> OnPostFido2OptionsAsync()
    {
        var authData = _partialAuth.Get(HttpContext);
        if (authData == null) return Unauthorized();

        var options = await _fido2Service.GetAuthenticationOptionsAsync(authData.UserId);
        HttpContext.Session.SetString("fido2_auth_options", JsonSerializer.Serialize(options));

        return new JsonResult(options);
    }

    // FIDO2 authentication - verify (AJAX)
    public async Task<IActionResult> OnPostFido2VerifyAsync([FromBody] Fido2AuthRequest request)
    {
        var authData = _partialAuth.Get(HttpContext);
        if (authData == null) return Unauthorized();

        var optionsJson = HttpContext.Session.GetString("fido2_auth_options");
        if (string.IsNullOrEmpty(optionsJson)) return BadRequest("Session expired");

        var options = JsonSerializer.Deserialize<AssertionOptions>(optionsJson);
        if (options == null) return BadRequest();

        var success = await _fido2Service.CompleteAuthenticationAsync(
            authData.UserId, request.AssertionResponse, options);

        if (!success)
        {
            await _auditService.LogAsync(authData.UserId, authData.Username, false, "FIDO2", "Verification failed", HttpContext);
            return BadRequest(new { error = "Authentication failed" });
        }

        var user = await _db.AdminUsers.FindAsync(authData.UserId);
        if (user == null) return BadRequest();

        await _auditService.LogAsync(user.Id, user.Username, true, "FIDO2", null, HttpContext);
        await SignInAsync(user);
        _partialAuth.Clear(HttpContext);

        return new JsonResult(new { success = true, redirectUrl = authData.ReturnUrl });
    }

    private async Task<IActionResult> CompleteLoginAsync(Models.AdminUser user, string returnUrl)
    {
        await SignInAsync(user);
        _partialAuth.Clear(HttpContext);
        return LocalRedirect(returnUrl);
    }

    private async Task SignInAsync(Models.AdminUser user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, "Admin"),
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal,
            new AuthenticationProperties
            {
                IsPersistent = false,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8),
            });

        user.LastLoginAt = DateTime.UtcNow;
        user.LastLoginIp = HttpContext.Connection.RemoteIpAddress?.ToString();
        user.FailedLoginAttempts = 0;
        user.AccountLocked = false;
        user.LockedUntil = null;
        user.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }
}

public class Fido2AuthRequest
{
    public AuthenticatorAssertionRawResponse AssertionResponse { get; set; } = null!;
}
