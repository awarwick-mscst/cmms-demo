namespace CMMS.Core.Configuration;

/// <summary>
/// Configuration settings for LDAP/Active Directory authentication.
/// </summary>
public class LdapSettings
{
    public const string SectionName = "LdapSettings";

    /// <summary>
    /// Enable or disable LDAP authentication. Default is false.
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// LDAP server hostname or IP address.
    /// </summary>
    public string Server { get; set; } = string.Empty;

    /// <summary>
    /// LDAP server port. Default is 389 (or 636 for SSL).
    /// </summary>
    public int Port { get; set; } = 389;

    /// <summary>
    /// Use SSL/TLS for the connection (LDAPS on port 636).
    /// </summary>
    public bool UseSsl { get; set; } = false;

    /// <summary>
    /// Use StartTLS to upgrade the connection to TLS.
    /// </summary>
    public bool UseStartTls { get; set; } = true;

    /// <summary>
    /// Base Distinguished Name for the LDAP directory.
    /// </summary>
    public string BaseDn { get; set; } = string.Empty;

    /// <summary>
    /// Search base for user lookups (e.g., "OU=Users,DC=example,DC=com").
    /// </summary>
    public string UserSearchBase { get; set; } = string.Empty;

    /// <summary>
    /// LDAP filter for user search. Use {0} as placeholder for username.
    /// Default is "(sAMAccountName={0})" for Active Directory.
    /// </summary>
    public string UserSearchFilter { get; set; } = "(sAMAccountName={0})";

    /// <summary>
    /// Windows domain name (e.g., "EXAMPLE" for EXAMPLE\username).
    /// </summary>
    public string Domain { get; set; } = string.Empty;

    /// <summary>
    /// Distinguished Name of the service account for LDAP queries.
    /// </summary>
    public string ServiceAccountDn { get; set; } = string.Empty;

    /// <summary>
    /// Password for the service account. Store securely in production!
    /// </summary>
    public string ServiceAccountPassword { get; set; } = string.Empty;

    /// <summary>
    /// Connection timeout in seconds.
    /// </summary>
    public int ConnectionTimeout { get; set; } = 30;

    /// <summary>
    /// Authentication mode: LdapOnly, LocalOnly, or Mixed.
    /// </summary>
    public AuthenticationMode AuthenticationMode { get; set; } = AuthenticationMode.Mixed;

    /// <summary>
    /// Allow fallback to local authentication if LDAP fails.
    /// Only applicable in Mixed mode.
    /// </summary>
    public bool AllowLocalFallback { get; set; } = true;

    /// <summary>
    /// Automatically sync user attributes from LDAP on login.
    /// </summary>
    public bool SyncUserAttributes { get; set; } = true;

    /// <summary>
    /// Mapping of user attributes from LDAP to local fields.
    /// </summary>
    public LdapAttributeMappings AttributeMappings { get; set; } = new();

    /// <summary>
    /// Mapping of LDAP groups to application roles.
    /// </summary>
    public List<LdapGroupMapping> GroupMappings { get; set; } = new();

    /// <summary>
    /// Default roles assigned to LDAP users if no group mappings match.
    /// </summary>
    public List<string> DefaultRoles { get; set; } = new() { "Viewer" };
}

/// <summary>
/// Authentication mode determining how users are authenticated.
/// </summary>
public enum AuthenticationMode
{
    /// <summary>
    /// Only LDAP authentication is allowed. Local passwords are ignored.
    /// </summary>
    LdapOnly,

    /// <summary>
    /// Only local authentication is allowed. LDAP is not used.
    /// </summary>
    LocalOnly,

    /// <summary>
    /// Both LDAP and local authentication are available.
    /// User's AuthenticationType determines which method is used.
    /// </summary>
    Mixed
}

/// <summary>
/// Mapping of LDAP attributes to local user properties.
/// </summary>
public class LdapAttributeMappings
{
    public string Username { get; set; } = "sAMAccountName";
    public string Email { get; set; } = "mail";
    public string FirstName { get; set; } = "givenName";
    public string LastName { get; set; } = "sn";
    public string Phone { get; set; } = "telephoneNumber";
    public string DisplayName { get; set; } = "displayName";
    public string MemberOf { get; set; } = "memberOf";
}

/// <summary>
/// Mapping of an LDAP group to an application role.
/// </summary>
public class LdapGroupMapping
{
    /// <summary>
    /// Full Distinguished Name of the LDAP group.
    /// </summary>
    public string LdapGroup { get; set; } = string.Empty;

    /// <summary>
    /// Name of the application role to assign.
    /// </summary>
    public string RoleName { get; set; } = string.Empty;
}
