namespace CMMS.Shared.DTOs;

/// <summary>
/// Response for LDAP status endpoint.
/// </summary>
public class LdapStatusResponse
{
    public bool Enabled { get; set; }
    public string? Server { get; set; }
    public int Port { get; set; }
    public bool UseSsl { get; set; }
    public bool UseStartTls { get; set; }
    public string? AuthenticationMode { get; set; }
    public bool AllowLocalFallback { get; set; }
    public bool SyncUserAttributes { get; set; }
    public int GroupMappingsCount { get; set; }
    public List<string> DefaultRoles { get; set; } = new();
    public List<string> ConfigurationWarnings { get; set; } = new();
}

/// <summary>
/// Response for LDAP connection test endpoint.
/// </summary>
public class LdapTestConnectionResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? ServerInfo { get; set; }
    public double ResponseTimeMs { get; set; }
    public DateTime TestedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Request to test LDAP user authentication.
/// </summary>
public class LdapTestUserRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// Response for LDAP user authentication test.
/// </summary>
public class LdapTestUserResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public LdapUserInfoDto? UserInfo { get; set; }
    public List<string> Groups { get; set; } = new();
    public List<string> MappedRoles { get; set; } = new();
    public DateTime TestedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// LDAP user information DTO.
/// </summary>
public class LdapUserInfoDto
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
/// Request to lookup a user in LDAP.
/// </summary>
public class LdapLookupUserRequest
{
    public string Username { get; set; } = string.Empty;
}

/// <summary>
/// Response for LDAP user lookup.
/// </summary>
public class LdapLookupUserResponse
{
    public bool Found { get; set; }
    public string? Message { get; set; }
    public LdapUserInfoDto? UserInfo { get; set; }
    public List<string> Groups { get; set; } = new();
    public bool ExistsLocally { get; set; }
    public int? LocalUserId { get; set; }
}
