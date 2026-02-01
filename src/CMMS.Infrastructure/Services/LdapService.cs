using System.Diagnostics;
using System.DirectoryServices.Protocols;
using System.Net;
using CMMS.Core.Configuration;
using CMMS.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CMMS.Infrastructure.Services;

/// <summary>
/// LDAP/Active Directory authentication service implementation.
/// Uses System.DirectoryServices.Protocols for cross-platform compatibility.
/// </summary>
public class LdapService : ILdapService
{
    private readonly LdapSettings _settings;
    private readonly ILogger<LdapService> _logger;

    public LdapService(IOptions<LdapSettings> settings, ILogger<LdapService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public bool IsEnabled => _settings.Enabled;

    public async Task<LdapAuthResult> AuthenticateAsync(string username, string password)
    {
        if (!_settings.Enabled)
        {
            return LdapAuthResult.Failed("LDAP authentication is not enabled");
        }

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            return LdapAuthResult.Failed("Username and password are required");
        }

        var validation = ValidateConfiguration();
        if (!validation.IsValid)
        {
            _logger.LogError("LDAP configuration is invalid: {Errors}", string.Join(", ", validation.Errors));
            return LdapAuthResult.Failed("LDAP is not properly configured");
        }

        try
        {
            // First, look up the user using the service account
            var userInfo = await GetUserAsync(username);
            if (userInfo == null)
            {
                _logger.LogWarning("LDAP user not found: {Username}", username);
                return LdapAuthResult.Failed("User not found in directory");
            }

            // Attempt to bind with the user's credentials to validate password
            using var connection = CreateConnection();

            // Determine the bind DN - use the full DN if available, or construct from domain
            var bindDn = userInfo.DistinguishedName;
            if (string.IsNullOrEmpty(bindDn) && !string.IsNullOrEmpty(_settings.Domain))
            {
                bindDn = $"{_settings.Domain}\\{username}";
            }

            var credential = new NetworkCredential(bindDn, password);
            connection.Credential = credential;
            connection.AuthType = AuthType.Basic;

            await Task.Run(() => connection.Bind());

            _logger.LogInformation("LDAP authentication successful for user: {Username}", username);

            // Get user's groups
            var groups = await GetUserGroupsAsync(userInfo.DistinguishedName);

            return LdapAuthResult.Succeeded(userInfo, groups);
        }
        catch (LdapException ex) when (ex.ErrorCode == 49) // Invalid credentials
        {
            _logger.LogWarning("LDAP authentication failed for user {Username}: Invalid credentials", username);
            return LdapAuthResult.Failed("Invalid username or password");
        }
        catch (LdapException ex)
        {
            _logger.LogError(ex, "LDAP error during authentication for user {Username}: {Message}", username, ex.Message);
            return LdapAuthResult.Failed($"LDAP error: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during LDAP authentication for user {Username}", username);
            return LdapAuthResult.Failed("An unexpected error occurred during authentication");
        }
    }

    public async Task<LdapUserInfo?> GetUserAsync(string username)
    {
        if (!_settings.Enabled)
        {
            return null;
        }

        try
        {
            using var connection = CreateConnection();
            BindWithServiceAccount(connection);

            var searchBase = string.IsNullOrEmpty(_settings.UserSearchBase) ? _settings.BaseDn : _settings.UserSearchBase;
            var filter = string.Format(_settings.UserSearchFilter, EscapeLdapFilterValue(username));

            var searchRequest = new SearchRequest(
                searchBase,
                filter,
                SearchScope.Subtree,
                GetUserAttributes());

            var response = await Task.Run(() =>
                (SearchResponse)connection.SendRequest(searchRequest));

            if (response.Entries.Count == 0)
            {
                return null;
            }

            var entry = response.Entries[0];
            return MapToUserInfo(entry);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error looking up LDAP user: {Username}", username);
            return null;
        }
    }

    public async Task<IReadOnlyList<string>> GetUserGroupsAsync(string userDn)
    {
        if (!_settings.Enabled || string.IsNullOrEmpty(userDn))
        {
            return Array.Empty<string>();
        }

        try
        {
            using var connection = CreateConnection();
            BindWithServiceAccount(connection);

            // Search for the user and get memberOf attribute
            var searchRequest = new SearchRequest(
                userDn,
                "(objectClass=*)",
                SearchScope.Base,
                _settings.AttributeMappings.MemberOf);

            var response = await Task.Run(() =>
                (SearchResponse)connection.SendRequest(searchRequest));

            if (response.Entries.Count == 0)
            {
                return Array.Empty<string>();
            }

            var entry = response.Entries[0];
            var memberOf = entry.Attributes[_settings.AttributeMappings.MemberOf];

            if (memberOf == null)
            {
                return Array.Empty<string>();
            }

            var groups = new List<string>();
            foreach (var value in memberOf.GetValues(typeof(string)))
            {
                if (value is string groupDn)
                {
                    groups.Add(groupDn);
                }
            }

            return groups;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting LDAP groups for user: {UserDn}", userDn);
            return Array.Empty<string>();
        }
    }

    public async Task<LdapConnectionTestResult> TestConnectionAsync()
    {
        if (!_settings.Enabled)
        {
            return LdapConnectionTestResult.Failed("LDAP is not enabled");
        }

        var validation = ValidateConfiguration();
        if (!validation.IsValid)
        {
            return LdapConnectionTestResult.Failed($"Configuration errors: {string.Join(", ", validation.Errors)}");
        }

        var stopwatch = Stopwatch.StartNew();

        try
        {
            using var connection = CreateConnection();
            BindWithServiceAccount(connection);

            // Perform a simple search to verify connectivity
            var searchRequest = new SearchRequest(
                _settings.BaseDn,
                "(objectClass=*)",
                SearchScope.Base,
                "defaultNamingContext");

            var response = await Task.Run(() =>
                (SearchResponse)connection.SendRequest(searchRequest));

            stopwatch.Stop();

            var serverInfo = $"Connected to {_settings.Server}:{_settings.Port}";
            if (_settings.UseSsl)
            {
                serverInfo += " (SSL)";
            }
            else if (_settings.UseStartTls)
            {
                serverInfo += " (StartTLS)";
            }

            _logger.LogInformation("LDAP connection test successful: {ServerInfo}", serverInfo);
            return LdapConnectionTestResult.Succeeded(serverInfo, stopwatch.Elapsed);
        }
        catch (LdapException ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "LDAP connection test failed: {Message}", ex.Message);
            return LdapConnectionTestResult.Failed($"LDAP error ({ex.ErrorCode}): {ex.Message}");
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "LDAP connection test failed with unexpected error");
            return LdapConnectionTestResult.Failed($"Connection failed: {ex.Message}");
        }
    }

    public LdapConfigurationValidation ValidateConfiguration()
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        if (string.IsNullOrWhiteSpace(_settings.Server))
        {
            errors.Add("LDAP Server is required");
        }

        if (string.IsNullOrWhiteSpace(_settings.BaseDn))
        {
            errors.Add("Base DN is required");
        }

        if (string.IsNullOrWhiteSpace(_settings.ServiceAccountDn))
        {
            errors.Add("Service Account DN is required");
        }

        if (string.IsNullOrWhiteSpace(_settings.ServiceAccountPassword))
        {
            errors.Add("Service Account Password is required");
        }

        if (_settings.Port <= 0 || _settings.Port > 65535)
        {
            errors.Add("Port must be between 1 and 65535");
        }

        if (!_settings.UseSsl && !_settings.UseStartTls)
        {
            warnings.Add("Neither SSL nor StartTLS is enabled. Connection will be unencrypted.");
        }

        if (_settings.GroupMappings.Count == 0 && _settings.DefaultRoles.Count == 0)
        {
            warnings.Add("No group mappings or default roles configured. LDAP users will have no roles.");
        }

        return new LdapConfigurationValidation
        {
            IsValid = errors.Count == 0,
            Errors = errors,
            Warnings = warnings
        };
    }

    private LdapConnection CreateConnection()
    {
        var identifier = new LdapDirectoryIdentifier(_settings.Server, _settings.Port);
        var connection = new LdapConnection(identifier)
        {
            Timeout = TimeSpan.FromSeconds(_settings.ConnectionTimeout)
        };

        connection.SessionOptions.ProtocolVersion = 3;
        connection.SessionOptions.ReferralChasing = ReferralChasingOptions.None;

        if (_settings.UseSsl)
        {
            connection.SessionOptions.SecureSocketLayer = true;
        }
        else if (_settings.UseStartTls)
        {
            connection.SessionOptions.StartTransportLayerSecurity(null);
        }

        return connection;
    }

    private void BindWithServiceAccount(LdapConnection connection)
    {
        var credential = new NetworkCredential(_settings.ServiceAccountDn, _settings.ServiceAccountPassword);
        connection.Credential = credential;
        connection.AuthType = AuthType.Basic;
        connection.Bind();
    }

    private string[] GetUserAttributes()
    {
        return new[]
        {
            "distinguishedName",
            _settings.AttributeMappings.Username,
            _settings.AttributeMappings.Email,
            _settings.AttributeMappings.FirstName,
            _settings.AttributeMappings.LastName,
            _settings.AttributeMappings.Phone,
            _settings.AttributeMappings.DisplayName,
            _settings.AttributeMappings.MemberOf
        };
    }

    private LdapUserInfo MapToUserInfo(SearchResultEntry entry)
    {
        return new LdapUserInfo
        {
            DistinguishedName = entry.DistinguishedName,
            Username = GetAttributeValue(entry, _settings.AttributeMappings.Username) ?? string.Empty,
            Email = GetAttributeValue(entry, _settings.AttributeMappings.Email) ?? string.Empty,
            FirstName = GetAttributeValue(entry, _settings.AttributeMappings.FirstName) ?? string.Empty,
            LastName = GetAttributeValue(entry, _settings.AttributeMappings.LastName) ?? string.Empty,
            Phone = GetAttributeValue(entry, _settings.AttributeMappings.Phone),
            DisplayName = GetAttributeValue(entry, _settings.AttributeMappings.DisplayName)
        };
    }

    private static string? GetAttributeValue(SearchResultEntry entry, string attributeName)
    {
        var attribute = entry.Attributes[attributeName];
        if (attribute == null || attribute.Count == 0)
        {
            return null;
        }

        var value = attribute[0];
        return value is byte[] bytes ? System.Text.Encoding.UTF8.GetString(bytes) : value?.ToString();
    }

    private static string EscapeLdapFilterValue(string value)
    {
        // Escape special characters in LDAP filter values
        return value
            .Replace("\\", "\\5c")
            .Replace("*", "\\2a")
            .Replace("(", "\\28")
            .Replace(")", "\\29")
            .Replace("\0", "\\00");
    }
}
