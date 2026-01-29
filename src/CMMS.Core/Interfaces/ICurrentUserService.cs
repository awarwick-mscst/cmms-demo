namespace CMMS.Core.Interfaces;

public interface ICurrentUserService
{
    int? UserId { get; }
    string? Username { get; }
    IEnumerable<string> Roles { get; }
    IEnumerable<string> Permissions { get; }
    bool IsAuthenticated { get; }
    bool HasPermission(string permission);
    bool IsInRole(string role);
}
