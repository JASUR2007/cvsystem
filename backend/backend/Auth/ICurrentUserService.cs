using System.Security.Claims;

namespace backend.Auth;

public interface ICurrentUserService {
    Guid? UserId { get; }
    Guid RequireUserId();
    bool IsAuthenticated { get; }
    bool IsInRole(string role);
    bool IsAdmin { get; }
    bool IsRecruiter { get; }
    bool IsCandidate { get; }
    ClaimsPrincipal? Principal { get; }
}
