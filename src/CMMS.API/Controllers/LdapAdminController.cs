using CMMS.Core.Configuration;
using CMMS.Core.Interfaces;
using CMMS.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace CMMS.API.Controllers;

/// <summary>
/// Administrative endpoints for LDAP/Active Directory configuration and testing.
/// </summary>
[ApiController]
[Route("api/v1/admin/ldap")]
[Authorize(Roles = "Administrator")]
public class LdapAdminController : ControllerBase
{
    private readonly ILdapService _ldapService;
    private readonly LdapSettings _ldapSettings;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<LdapAdminController> _logger;

    public LdapAdminController(
        ILdapService ldapService,
        IOptions<LdapSettings> ldapSettings,
        IUnitOfWork unitOfWork,
        ILogger<LdapAdminController> logger)
    {
        _ldapService = ldapService;
        _ldapSettings = ldapSettings.Value;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// Get current LDAP configuration status.
    /// </summary>
    [HttpGet("status")]
    [ProducesResponseType(typeof(ApiResponse<LdapStatusResponse>), StatusCodes.Status200OK)]
    public ActionResult<ApiResponse<LdapStatusResponse>> GetStatus()
    {
        var validation = _ldapService.ValidateConfiguration();

        var status = new LdapStatusResponse
        {
            Enabled = _ldapSettings.Enabled,
            Server = _ldapSettings.Enabled ? MaskSensitiveInfo(_ldapSettings.Server) : null,
            Port = _ldapSettings.Port,
            UseSsl = _ldapSettings.UseSsl,
            UseStartTls = _ldapSettings.UseStartTls,
            AuthenticationMode = _ldapSettings.AuthenticationMode.ToString(),
            AllowLocalFallback = _ldapSettings.AllowLocalFallback,
            SyncUserAttributes = _ldapSettings.SyncUserAttributes,
            GroupMappingsCount = _ldapSettings.GroupMappings.Count,
            DefaultRoles = _ldapSettings.DefaultRoles,
            ConfigurationWarnings = validation.Warnings
        };

        return Ok(ApiResponse<LdapStatusResponse>.Ok(status));
    }

    /// <summary>
    /// Test LDAP server connection using the service account.
    /// </summary>
    [HttpPost("test-connection")]
    [ProducesResponseType(typeof(ApiResponse<LdapTestConnectionResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<LdapTestConnectionResponse>>> TestConnection()
    {
        if (!_ldapSettings.Enabled)
        {
            return Ok(ApiResponse<LdapTestConnectionResponse>.Fail("LDAP is not enabled"));
        }

        _logger.LogInformation("Admin testing LDAP connection");

        var result = await _ldapService.TestConnectionAsync();

        var response = new LdapTestConnectionResponse
        {
            Success = result.Success,
            Message = result.Success ? "Connection successful" : result.Error,
            ServerInfo = result.ServerInfo,
            ResponseTimeMs = result.ResponseTime.TotalMilliseconds,
            TestedAt = DateTime.UtcNow
        };

        if (result.Success)
        {
            return Ok(ApiResponse<LdapTestConnectionResponse>.Ok(response, "LDAP connection test successful"));
        }

        return Ok(ApiResponse<LdapTestConnectionResponse>.Ok(response, result.Error));
    }

    /// <summary>
    /// Test LDAP authentication for a specific user.
    /// </summary>
    [HttpPost("test-user")]
    [ProducesResponseType(typeof(ApiResponse<LdapTestUserResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<LdapTestUserResponse>>> TestUserAuthentication(
        [FromBody] LdapTestUserRequest request)
    {
        if (!_ldapSettings.Enabled)
        {
            return Ok(ApiResponse<LdapTestUserResponse>.Fail("LDAP is not enabled"));
        }

        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(ApiResponse<LdapTestUserResponse>.Fail("Username and password are required"));
        }

        _logger.LogInformation("Admin testing LDAP authentication for user: {Username}", request.Username);

        var result = await _ldapService.AuthenticateAsync(request.Username, request.Password);

        var response = new LdapTestUserResponse
        {
            Success = result.Success,
            Message = result.Success ? "Authentication successful" : result.Error,
            TestedAt = DateTime.UtcNow
        };

        if (result.Success && result.UserInfo != null)
        {
            response.UserInfo = new LdapUserInfoDto
            {
                DistinguishedName = result.UserInfo.DistinguishedName,
                Username = result.UserInfo.Username,
                Email = result.UserInfo.Email,
                FirstName = result.UserInfo.FirstName,
                LastName = result.UserInfo.LastName,
                Phone = result.UserInfo.Phone,
                DisplayName = result.UserInfo.DisplayName
            };

            response.Groups = result.Groups.ToList();

            // Map groups to roles
            response.MappedRoles = MapGroupsToRoles(result.Groups);
        }

        return Ok(ApiResponse<LdapTestUserResponse>.Ok(response));
    }

    /// <summary>
    /// Look up a user in LDAP without authenticating.
    /// </summary>
    [HttpPost("lookup-user")]
    [ProducesResponseType(typeof(ApiResponse<LdapLookupUserResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<LdapLookupUserResponse>>> LookupUser(
        [FromBody] LdapLookupUserRequest request)
    {
        if (!_ldapSettings.Enabled)
        {
            return Ok(ApiResponse<LdapLookupUserResponse>.Fail("LDAP is not enabled"));
        }

        if (string.IsNullOrWhiteSpace(request.Username))
        {
            return BadRequest(ApiResponse<LdapLookupUserResponse>.Fail("Username is required"));
        }

        _logger.LogInformation("Admin looking up LDAP user: {Username}", request.Username);

        var ldapUser = await _ldapService.GetUserAsync(request.Username);

        var response = new LdapLookupUserResponse
        {
            Found = ldapUser != null,
            Message = ldapUser != null ? "User found in directory" : "User not found in directory"
        };

        if (ldapUser != null)
        {
            response.UserInfo = new LdapUserInfoDto
            {
                DistinguishedName = ldapUser.DistinguishedName,
                Username = ldapUser.Username,
                Email = ldapUser.Email,
                FirstName = ldapUser.FirstName,
                LastName = ldapUser.LastName,
                Phone = ldapUser.Phone,
                DisplayName = ldapUser.DisplayName
            };

            // Get groups
            var groups = await _ldapService.GetUserGroupsAsync(ldapUser.DistinguishedName);
            response.Groups = groups.ToList();

            // Check if user exists locally
            var localUser = await _unitOfWork.Users
                .FirstOrDefaultAsync(u => u.Username == request.Username);

            response.ExistsLocally = localUser != null;
            response.LocalUserId = localUser?.Id;
        }

        return Ok(ApiResponse<LdapLookupUserResponse>.Ok(response));
    }

    /// <summary>
    /// Get the list of configured group mappings.
    /// </summary>
    [HttpGet("group-mappings")]
    [ProducesResponseType(typeof(ApiResponse<List<LdapGroupMapping>>), StatusCodes.Status200OK)]
    public ActionResult<ApiResponse<List<LdapGroupMapping>>> GetGroupMappings()
    {
        if (!_ldapSettings.Enabled)
        {
            return Ok(ApiResponse<List<LdapGroupMapping>>.Fail("LDAP is not enabled"));
        }

        return Ok(ApiResponse<List<LdapGroupMapping>>.Ok(_ldapSettings.GroupMappings));
    }

    private List<string> MapGroupsToRoles(IReadOnlyList<string> ldapGroups)
    {
        var roles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var groupDn in ldapGroups)
        {
            var mapping = _ldapSettings.GroupMappings
                .FirstOrDefault(m => string.Equals(m.LdapGroup, groupDn, StringComparison.OrdinalIgnoreCase));

            if (mapping != null)
            {
                roles.Add(mapping.RoleName);
            }
        }

        // If no groups matched, return default roles
        if (roles.Count == 0)
        {
            return _ldapSettings.DefaultRoles;
        }

        return roles.ToList();
    }

    private static string? MaskSensitiveInfo(string? value)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= 4)
        {
            return value;
        }

        // Show first 2 and last 2 characters
        return $"{value[..2]}***{value[^2..]}";
    }
}
