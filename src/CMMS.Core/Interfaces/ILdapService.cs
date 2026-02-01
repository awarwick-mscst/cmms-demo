using CMMS.Core.Entities;

namespace CMMS.Core.Interfaces;

/// <summary>
/// Service interface for LDAP/Active Directory operations.
/// </summary>
public interface ILdapService
{
    /// <summary>
    /// Gets whether LDAP authentication is enabled.
    /// </summary>
    bool IsEnabled { get; }

    /// <summary>
    /// Authenticates a user against the LDAP server.
    /// </summary>
    /// <param name="username">Username (sAMAccountName or UPN).</param>
    /// <param name="password">User's password.</param>
    /// <returns>Authentication result with user info if successful.</returns>
    Task<LdapAuthResult> AuthenticateAsync(string username, string password);

    /// <summary>
    /// Looks up a user in LDAP by username.
    /// </summary>
    /// <param name="username">Username to search for.</param>
    /// <returns>LDAP user info if found, null otherwise.</returns>
    Task<LdapUserInfo?> GetUserAsync(string username);

    /// <summary>
    /// Gets the groups a user belongs to.
    /// </summary>
    /// <param name="userDn">Distinguished Name of the user.</param>
    /// <returns>List of group Distinguished Names.</returns>
    Task<IReadOnlyList<string>> GetUserGroupsAsync(string userDn);

    /// <summary>
    /// Tests the LDAP connection using the configured service account.
    /// </summary>
    /// <returns>Connection test result.</returns>
    Task<LdapConnectionTestResult> TestConnectionAsync();

    /// <summary>
    /// Validates that the LDAP configuration is complete.
    /// </summary>
    /// <returns>Validation result with any errors.</returns>
    LdapConfigurationValidation ValidateConfiguration();
}

/// <summary>
/// Result of an LDAP authentication attempt.
/// </summary>
public class LdapAuthResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public LdapUserInfo? UserInfo { get; set; }
    public IReadOnlyList<string> Groups { get; set; } = Array.Empty<string>();

    public static LdapAuthResult Failed(string error) => new() { Success = false, Error = error };
    public static LdapAuthResult Succeeded(LdapUserInfo userInfo, IReadOnlyList<string> groups) =>
        new() { Success = true, UserInfo = userInfo, Groups = groups };
}

/// <summary>
/// User information retrieved from LDAP.
/// </summary>
public class LdapUserInfo
{
    public string DistinguishedName { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? DisplayName { get; set; }
}

/// <summary>
/// Result of an LDAP connection test.
/// </summary>
public class LdapConnectionTestResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public string? ServerInfo { get; set; }
    public TimeSpan ResponseTime { get; set; }

    public static LdapConnectionTestResult Failed(string error) =>
        new() { Success = false, Error = error };

    public static LdapConnectionTestResult Succeeded(string? serverInfo, TimeSpan responseTime) =>
        new() { Success = true, ServerInfo = serverInfo, ResponseTime = responseTime };
}

/// <summary>
/// Result of LDAP configuration validation.
/// </summary>
public class LdapConfigurationValidation
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();

    public static LdapConfigurationValidation Valid() => new() { IsValid = true };

    public static LdapConfigurationValidation Invalid(params string[] errors) =>
        new() { IsValid = false, Errors = errors.ToList() };
}
