using CMMS.Core.Interfaces;

namespace CMMS.Infrastructure.Services;

/// <summary>
/// Null implementation of ILdapService used when LDAP is disabled.
/// All operations return disabled/empty results.
/// </summary>
public class NullLdapService : ILdapService
{
    public bool IsEnabled => false;

    public Task<LdapAuthResult> AuthenticateAsync(string username, string password)
    {
        return Task.FromResult(LdapAuthResult.Failed("LDAP authentication is not enabled"));
    }

    public Task<LdapUserInfo?> GetUserAsync(string username)
    {
        return Task.FromResult<LdapUserInfo?>(null);
    }

    public Task<IReadOnlyList<string>> GetUserGroupsAsync(string userDn)
    {
        return Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());
    }

    public Task<LdapConnectionTestResult> TestConnectionAsync()
    {
        return Task.FromResult(LdapConnectionTestResult.Failed("LDAP is not enabled in configuration"));
    }

    public LdapConfigurationValidation ValidateConfiguration()
    {
        return new LdapConfigurationValidation
        {
            IsValid = false,
            Errors = new List<string> { "LDAP is not enabled" }
        };
    }
}
