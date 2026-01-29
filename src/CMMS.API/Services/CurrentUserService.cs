using System.Security.Claims;
using CMMS.Core.Interfaces;

namespace CMMS.API.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public int? UserId
    {
        get
        {
            var userId = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? _httpContextAccessor.HttpContext?.User.FindFirstValue("sub");

            return int.TryParse(userId, out var id) ? id : null;
        }
    }

    public string? Username =>
        _httpContextAccessor.HttpContext?.User.FindFirstValue("username");

    public IEnumerable<string> Roles =>
        _httpContextAccessor.HttpContext?.User.FindAll(ClaimTypes.Role).Select(c => c.Value)
        ?? Enumerable.Empty<string>();

    public IEnumerable<string> Permissions =>
        _httpContextAccessor.HttpContext?.User.FindAll("permission").Select(c => c.Value)
        ?? Enumerable.Empty<string>();

    public bool IsAuthenticated =>
        _httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;

    public bool HasPermission(string permission) =>
        Permissions.Contains(permission);

    public bool IsInRole(string role) =>
        Roles.Contains(role);
}
