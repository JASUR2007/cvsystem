using System.Security.Claims;
using backend.DTOs.Auth;

namespace backend.Services.Interfaces;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<CurrentUserResponse> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<AuthResponse> ExchangeExternalCodeAsync(string code, CancellationToken cancellationToken = default);
    object GetExternalProvidersStatus();
    Task<string> ProcessExternalLoginAsync(string scheme, ClaimsPrincipal principal, CancellationToken cancellationToken = default);
}
