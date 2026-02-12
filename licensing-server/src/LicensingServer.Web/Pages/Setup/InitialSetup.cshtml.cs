using System.Text.Json;
using Fido2NetLib;
using LicensingServer.Web.Data;
using LicensingServer.Web.Models;
using LicensingServer.Web.Services.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace LicensingServer.Web.Pages.Setup;

[AllowAnonymous]
public class InitialSetupModel : PageModel
{
    private readonly LicensingDbContext _db;
    private readonly PasswordHashingService _passwordHasher;
    private readonly TotpService _totpService;
    private readonly Fido2AuthService _fido2Service;
    private readonly AdminAuditService _auditService;
    private readonly ILogger<InitialSetupModel> _logger;

    public InitialSetupModel(
        LicensingDbContext db,
        PasswordHashingService passwordHasher,
        TotpService totpService,
        Fido2AuthService fido2Service,
        AdminAuditService auditService,
        ILogger<InitialSetupModel> logger)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _totpService = totpService;
        _fido2Service = fido2Service;
        _auditService = auditService;
        _logger = logger;
    }

    [BindProperty] public string Username { get; set; } = "admin";
    [BindProperty] public string Email { get; set; } = string.Empty;
    [BindProperty] public string Password { get; set; } = string.Empty;
    [BindProperty] public string ConfirmPassword { get; set; } = string.Empty;
    [BindProperty] public string TotpCode { get; set; } = string.Empty;
    [BindProperty] public string? Fido2DeviceName { get; set; }

    public int Step { get; set; } = 1;
    public string? ErrorMessage { get; set; }
    public string? TotpSecret { get; set; }
    public string? QrCodeBase64 { get; set; }
    public List<string>? RecoveryCodes { get; set; }
    public bool SetupComplete { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (await _db.AdminUsers.AnyAsync())
            return NotFound();

        return Page();
    }

    // Step 1: Create account
    public async Task<IActionResult> OnPostCreateAccountAsync()
    {
        if (await _db.AdminUsers.AnyAsync())
            return NotFound();

        if (string.IsNullOrWhiteSpace(Username) || Username.Length < 3)
        {
            ErrorMessage = "Username must be at least 3 characters.";
            Step = 1;
            return Page();
        }

        if (string.IsNullOrWhiteSpace(Email) || !Email.Contains('@'))
        {
            ErrorMessage = "Please enter a valid email address.";
            Step = 1;
            return Page();
        }

        if (string.IsNullOrWhiteSpace(Password) || Password.Length < 12)
        {
            ErrorMessage = "Password must be at least 12 characters.";
            Step = 1;
            return Page();
        }

        if (Password != ConfirmPassword)
        {
            ErrorMessage = "Passwords do not match.";
            Step = 1;
            return Page();
        }

        // Check password complexity
        if (!HasPasswordComplexity(Password))
        {
            ErrorMessage = "Password must contain uppercase, lowercase, number, and special character.";
            Step = 1;
            return Page();
        }

        // Store in session for next step
        HttpContext.Session.SetString("setup_username", Username);
        HttpContext.Session.SetString("setup_email", Email);
        HttpContext.Session.SetString("setup_password_hash", _passwordHasher.HashPassword(Password));

        // Generate TOTP secret for step 2
        var secret = _totpService.GenerateSecret();
        HttpContext.Session.SetString("setup_totp_secret", secret);

        var qrUri = _totpService.GenerateQrCodeUri(Username, secret);
        var qrPng = _totpService.GenerateQrCodePng(qrUri);

        TotpSecret = secret;
        QrCodeBase64 = Convert.ToBase64String(qrPng);
        Step = 2;

        return Page();
    }

    // Step 2: Verify TOTP
    public async Task<IActionResult> OnPostVerifyTotpAsync()
    {
        if (await _db.AdminUsers.AnyAsync())
            return NotFound();

        var secret = HttpContext.Session.GetString("setup_totp_secret");
        if (string.IsNullOrEmpty(secret))
        {
            ErrorMessage = "Session expired. Please start over.";
            Step = 1;
            return Page();
        }

        if (!_totpService.VerifyCode(secret, TotpCode))
        {
            // Re-display QR code
            var username = HttpContext.Session.GetString("setup_username") ?? "admin";
            var qrUri = _totpService.GenerateQrCodeUri(username, secret);
            var qrPng = _totpService.GenerateQrCodePng(qrUri);
            TotpSecret = secret;
            QrCodeBase64 = Convert.ToBase64String(qrPng);
            ErrorMessage = "Invalid TOTP code. Please try again.";
            Step = 2;
            return Page();
        }

        // TOTP verified — create the admin user
        var passwordHash = HttpContext.Session.GetString("setup_password_hash");
        var email = HttpContext.Session.GetString("setup_email");
        var uname = HttpContext.Session.GetString("setup_username");

        if (string.IsNullOrEmpty(passwordHash) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(uname))
        {
            ErrorMessage = "Session expired. Please start over.";
            Step = 1;
            return Page();
        }

        // Generate recovery codes
        var recoveryCodes = _totpService.GenerateRecoveryCodes();

        var adminUser = new AdminUser
        {
            Username = uname,
            Email = email,
            PasswordHash = passwordHash,
            RequireMfa = true,
            TotpEnabled = true,
            TotpSecretEncrypted = secret, // TODO: Encrypt with DPAPI in production
            RecoveryCodesEncrypted = JsonSerializer.Serialize(recoveryCodes),
            CreatedAt = DateTime.UtcNow,
        };

        _db.AdminUsers.Add(adminUser);
        await _db.SaveChangesAsync();

        await _auditService.LogAsync(adminUser.Id, uname, true, "Setup", null, HttpContext);

        // Clear session
        HttpContext.Session.Clear();

        _logger.LogInformation("Initial admin account created: {Username}", uname);

        RecoveryCodes = recoveryCodes;
        SetupComplete = true;
        Step = 3;

        return Page();
    }

    // FIDO2 registration endpoints (called via AJAX from step 3)
    public async Task<IActionResult> OnPostFido2OptionsAsync()
    {
        var user = await _db.AdminUsers.Include(u => u.Fido2Credentials).FirstOrDefaultAsync();
        if (user == null) return BadRequest();

        var existingCreds = user.Fido2Credentials.ToList();
        var options = _fido2Service.GetRegistrationOptions(user, existingCreds);

        HttpContext.Session.SetString("fido2_reg_options", JsonSerializer.Serialize(options));

        return new JsonResult(options);
    }

    public async Task<IActionResult> OnPostFido2RegisterAsync([FromBody] Fido2RegistrationRequest request)
    {
        var user = await _db.AdminUsers.FirstOrDefaultAsync();
        if (user == null) return BadRequest();

        var optionsJson = HttpContext.Session.GetString("fido2_reg_options");
        if (string.IsNullOrEmpty(optionsJson)) return BadRequest("Session expired");

        var options = JsonSerializer.Deserialize<CredentialCreateOptions>(optionsJson);
        if (options == null) return BadRequest();

        var credential = await _fido2Service.CompleteRegistrationAsync(
            user, request.AttestationResponse, options, request.DeviceName ?? "Security Key");

        if (credential == null)
            return BadRequest(new { error = "Registration failed" });

        await _auditService.LogAsync(user.Id, user.Username, true, "FIDO2-Registration", null, HttpContext);

        return new JsonResult(new { success = true, deviceName = credential.DeviceName });
    }

    private static bool HasPasswordComplexity(string password)
    {
        return password.Any(char.IsUpper) &&
               password.Any(char.IsLower) &&
               password.Any(char.IsDigit) &&
               password.Any(c => !char.IsLetterOrDigit(c));
    }
}

public class Fido2RegistrationRequest
{
    public AuthenticatorAttestationRawResponse AttestationResponse { get; set; } = null!;
    public string? DeviceName { get; set; }
}
