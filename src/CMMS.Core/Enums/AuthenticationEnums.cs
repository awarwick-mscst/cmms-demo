namespace CMMS.Core.Enums;

/// <summary>
/// Specifies how a user authenticates to the system.
/// </summary>
public enum AuthenticationType
{
    /// <summary>
    /// User authenticates with local password only.
    /// </summary>
    Local = 0,

    /// <summary>
    /// User authenticates via LDAP/Active Directory only.
    /// </summary>
    Ldap = 1,

    /// <summary>
    /// User can authenticate via either LDAP or local password.
    /// LDAP is tried first, then local password as fallback.
    /// </summary>
    Both = 2
}
