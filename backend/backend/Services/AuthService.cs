using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using backend.Auth;
using backend.Common.Exceptions;
using backend.Data;
using backend.DTOs.Auth;
using backend.Entities;
using backend.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace backend.Services;

public class AuthService(
    UserManager<AppUser> userManager,
    JwtTokenService tokenService,
    AppDbContext db,
    IConfiguration configuration) : IAuthService
{
    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var existing = await userManager.FindByEmailAsync(request.Email);
        if (existing is not null)
            throw new ConflictException("An account with this email already exists.");

        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            UserName = request.Email,
            Email = request.Email,
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim()
        };

        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            throw new ValidationException(string.Join(" ", result.Errors.Select(e => e.Description)));

        await userManager.AddToRoleAsync(user, Roles.Candidate);

        return await CreateAuthResponseAsync(user);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null || !await userManager.CheckPasswordAsync(user, request.Password))
            throw new ValidationException("Invalid email or password.");

        if (user.IsBlocked)
            throw new ForbiddenException("Account is unavailable.");

        return await CreateAuthResponseAsync(user, request.RememberMe);
    }

    public async Task<CurrentUserResponse> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null || user.IsBlocked)
            throw new NotFoundException("Account is unavailable.");

        var roles = await userManager.GetRolesAsync(user);
        return new CurrentUserResponse(user.Id, user.Email ?? string.Empty, user.FirstName, user.LastName, roles, user.PhotoObjectKey);
    }

    public async Task<AuthResponse> ExchangeExternalCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var hash = Hash(code);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var ticket = await db.ExternalAuthTickets.FirstOrDefaultAsync(item => item.CodeHash == hash, cancellationToken);
        if (ticket is null || ticket.ExpiresAt <= DateTime.UtcNow)
            throw new ValidationException("Sign-in link has expired or was already used.");

        var deleted = await db.ExternalAuthTickets.Where(item => item.Id == ticket.Id && item.ExpiresAt > DateTime.UtcNow)
            .ExecuteDeleteAsync(cancellationToken);
        if (deleted != 1)
            throw new ValidationException("Sign-in link has expired or was already used.");

        var user = await userManager.FindByIdAsync(ticket.UserId.ToString());
        if (user is null || user.IsBlocked)
            throw new ForbiddenException("Account is unavailable.");

        await transaction.CommitAsync(cancellationToken);

        return await CreateAuthResponseAsync(user, rememberMe: true);
    }

    public object GetExternalProvidersStatus() => new
    {
        google = !string.IsNullOrWhiteSpace(configuration["Authentication:Google:ClientId"]) &&
                 !string.IsNullOrWhiteSpace(configuration["Authentication:Google:ClientSecret"]),
        github = !string.IsNullOrWhiteSpace(configuration["Authentication:GitHub:ClientId"]) &&
                 !string.IsNullOrWhiteSpace(configuration["Authentication:GitHub:ClientSecret"])
    };

    public async Task<string> ProcessExternalLoginAsync(string scheme, ClaimsPrincipal principal, CancellationToken cancellationToken = default)
    {
        var externalId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        var email = principal.FindFirstValue(ClaimTypes.Email)?.Trim();
        if (string.IsNullOrWhiteSpace(externalId) || string.IsNullOrWhiteSpace(email))
            throw new ValidationException("email_required");

        var user = await userManager.FindByLoginAsync(scheme, externalId);
        if (user is null)
        {
            if (await userManager.FindByEmailAsync(email) is not null)
                throw new ConflictException("account_exists");

            var fullName = principal.FindFirstValue(ClaimTypes.Name)?.Trim() ?? "";
            var parts = fullName.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            user = new AppUser
            {
                Id = Guid.NewGuid(),
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FirstName = parts.Length > 0 ? parts[0][..Math.Min(parts[0].Length, 100)] : "User",
                LastName = parts.Length > 1 ? parts[1][..Math.Min(parts[1].Length, 100)] : ""
            };

            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            var created = await userManager.CreateAsync(user);
            if (!created.Succeeded) throw new ValidationException("account_creation_failed");
            var assigned = await userManager.AddToRoleAsync(user, Roles.Candidate);
            var linked = assigned.Succeeded
                ? await userManager.AddLoginAsync(user, new UserLoginInfo(scheme, externalId, scheme))
                : IdentityResult.Failed();
            if (!assigned.Succeeded || !linked.Succeeded) throw new ValidationException("account_creation_failed");
            await transaction.CommitAsync(cancellationToken);
        }

        if (user.IsBlocked)
            throw new ForbiddenException("account_blocked");

        var code = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        db.ExternalAuthTickets.Add(new ExternalAuthTicket
        {
            Id = Guid.NewGuid(),
            CodeHash = Hash(code),
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddMinutes(5)
        });
        await db.SaveChangesAsync(cancellationToken);

        return code;
    }

    private async Task<AuthResponse> CreateAuthResponseAsync(AppUser user, bool rememberMe = false)
    {
        var token = await tokenService.CreateAsync(user, rememberMe);
        var roles = await userManager.GetRolesAsync(user);
        var userResponse = new CurrentUserResponse(user.Id, user.Email ?? string.Empty, user.FirstName, user.LastName, roles, user.PhotoObjectKey);
        return new AuthResponse(token, userResponse);
    }

    private static string Hash(string code) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(code)));
}
