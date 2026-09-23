using System.Security.Claims;
using backend.Common.Exceptions;

namespace backend.Auth;

public class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    public ClaimsPrincipal? Principal => httpContextAccessor.HttpContext?.User;

    public Guid? UserId
    {
        get
        {
            var sub = Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                   ?? Principal?.FindFirst("sub")?.Value;
            return Guid.TryParse(sub, out var id) ? id : null;
        }
    }

    public Guid RequireUserId() =>
        UserId ?? throw new ForbiddenException("Authentication is required.");

    public bool IsAuthenticated =>
        Principal?.Identity?.IsAuthenticated ?? false;

    public bool IsInRole(string role) =>
        Principal?.IsInRole(role) ?? false;

    public bool IsAdmin => IsInRole(Roles.Administrator);

    public bool IsRecruiter => IsInRole(Roles.Recruiter);

    public bool IsCandidate => IsInRole(Roles.Candidate);
}
