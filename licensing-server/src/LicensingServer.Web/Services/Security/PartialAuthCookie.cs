using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;

namespace LicensingServer.Web.Services.Security;

public class PartialAuthCookie
{
    private const string CookieName = "CmmsLicensing.PartialAuth";
    private const string Purpose = "PartialAuth";
    private const int ExpirationMinutes = 5;
    private readonly IDataProtector _protector;
    private readonly ILogger<PartialAuthCookie> _logger;

    public PartialAuthCookie(IDataProtectionProvider provider, ILogger<PartialAuthCookie> logger)
    {
        _protector = provider.CreateProtector(Purpose);
        _logger = logger;
    }

    /// <summary>
    /// Creates an encrypted token containing the partial auth state.
    /// The Data Protection output is base64 — RedirectToPage will URL-encode it.
    /// </summary>
    public string CreateToken(int userId, string username, string returnUrl)
    {
        var payload = JsonSerializer.Serialize(new PartialAuthData
        {
            UserId = userId,
            Username = username,
            ReturnUrl = returnUrl,
            Timestamp = DateTime.UtcNow.ToString("O"),
        });

        var token = _protector.Protect(payload);
        _logger.LogInformation("Created partial auth token for user {Username} (userId={UserId}), token length={Length}",
            username, userId, token.Length);
        return token;
    }

    /// <summary>
    /// Reads and validates an encrypted token.
    /// </summary>
    public PartialAuthData? ReadToken(string token)
    {
        try
        {
            _logger.LogInformation("ReadToken called, token length={Length}", token.Length);
            var payload = _protector.Unprotect(token);
            var data = JsonSerializer.Deserialize<PartialAuthData>(payload);

            if (data == null)
            {
                _logger.LogWarning("ReadToken: deserialized data is null");
                return null;
            }

            if (DateTimeOffset.TryParse(data.Timestamp, out var timestamp))
            {
                if ((DateTimeOffset.UtcNow - timestamp).TotalMinutes > ExpirationMinutes)
                {
                    _logger.LogWarning("ReadToken: token expired (age={Age} min)", (DateTimeOffset.UtcNow - timestamp).TotalMinutes);
                    return null;
                }
            }

            _logger.LogInformation("ReadToken: success for userId={UserId}", data.UserId);
            return data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ReadToken: failed to decrypt/parse token");
            return null;
        }
    }

    /// <summary>
    /// Gets partial auth data from query string token OR cookie.
    /// </summary>
    public PartialAuthData? Get(HttpContext context)
    {
        // First try query string token (used for Login → LoginMfa redirect)
        var token = context.Request.Query["t"].FirstOrDefault();
        if (!string.IsNullOrEmpty(token))
        {
            _logger.LogInformation("Get: found query string token, length={Length}", token.Length);
            var data = ReadToken(token);
            if (data != null)
            {
                // Also set cookie so subsequent POST/AJAX calls from MFA page work
                SetCookie(context, data);
                return data;
            }
        }
        else
        {
            _logger.LogInformation("Get: no query string token found");
        }

        // Fall back to cookie
        if (!context.Request.Cookies.TryGetValue(CookieName, out var encrypted) || string.IsNullOrEmpty(encrypted))
        {
            _logger.LogWarning("Get: no cookie found either, returning null");
            return null;
        }

        try
        {
            _logger.LogInformation("Get: found cookie, attempting to decrypt");
            var payload = _protector.Unprotect(encrypted);
            var cookieData = JsonSerializer.Deserialize<PartialAuthData>(payload);

            if (cookieData == null) return null;

            if (DateTimeOffset.TryParse(cookieData.Timestamp, out var timestamp))
            {
                if ((DateTimeOffset.UtcNow - timestamp).TotalMinutes > ExpirationMinutes)
                {
                    Clear(context);
                    return null;
                }
            }

            return cookieData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Get: cookie decryption failed");
            Clear(context);
            return null;
        }
    }

    private void SetCookie(HttpContext context, PartialAuthData data)
    {
        var payload = JsonSerializer.Serialize(data);
        var encrypted = _protector.Protect(payload);

        context.Response.Cookies.Append(CookieName, encrypted, new CookieOptions
        {
            HttpOnly = true,
            SameSite = SameSiteMode.Lax,
            IsEssential = true,
            Expires = DateTimeOffset.UtcNow.AddMinutes(ExpirationMinutes),
            Path = "/",
        });
    }

    public void Set(HttpContext context, int userId, string username, string returnUrl)
    {
        SetCookie(context, new PartialAuthData
        {
            UserId = userId,
            Username = username,
            ReturnUrl = returnUrl,
            Timestamp = DateTime.UtcNow.ToString("O"),
        });
    }

    public void Clear(HttpContext context)
    {
        context.Response.Cookies.Delete(CookieName, new CookieOptions
        {
            HttpOnly = true,
            SameSite = SameSiteMode.Lax,
            Path = "/",
        });
    }
}

public class PartialAuthData
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string ReturnUrl { get; set; } = "/Dashboard";
    public string Timestamp { get; set; } = string.Empty;
}
