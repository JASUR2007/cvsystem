using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using backend.Auth;
using backend.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class ExternalAuthController(
    AppDbContext db,
    UserManager<AppUser> userManager,
    TokenService tokenService,
    IConfiguration configuration,
    IAuthenticationSchemeProvider schemes) : ControllerBase
{
    [HttpGet("providers")]
    public IActionResult Providers() => Ok(new
    {
        google = Configured("Google"),
        github = Configured("GitHub")
    });

    [HttpGet("external/{provider}")]
    public async Task<IActionResult> Start(string provider)
    {
        var scheme = Scheme(provider);
        if (scheme is null || !Configured(scheme) || await schemes.GetSchemeAsync(scheme) is null)
            return NotFound(new { message = "This sign-in provider is not available." });
        var callback = Url.Action(nameof(Callback), values: new { provider })!;
        var properties = new AuthenticationProperties { RedirectUri = callback };
        properties.Items["provider"] = scheme;
        return Challenge(properties, scheme);
    }

    [HttpGet("external/{provider}/callback")]
    public async Task<IActionResult> Callback(string provider, CancellationToken cancellationToken)
    {
        var scheme = Scheme(provider);
        if (scheme is null || !Configured(scheme)) return RedirectToFrontend("provider_unavailable");
        var authentication = await HttpContext.AuthenticateAsync("External");
        if (!authentication.Succeeded || authentication.Principal is null)
            return RedirectToFrontend("authentication_failed");
        if (authentication.Properties is null
            || !authentication.Properties.Items.TryGetValue("provider", out var originalProvider)
            || originalProvider != scheme)
            return RedirectToFrontend("authentication_failed");
        var principal = authentication.Principal;
        var externalId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        var email = principal.FindFirstValue(ClaimTypes.Email)?.Trim();
        if (string.IsNullOrWhiteSpace(externalId) || string.IsNullOrWhiteSpace(email))
            return RedirectToFrontend("email_required");
        var user = await userManager.FindByLoginAsync(scheme, externalId);
        if (user is null)
        {
            if (await userManager.FindByEmailAsync(email) is not null)
                return RedirectToFrontend("account_exists");
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
            if (!created.Succeeded) return RedirectToFrontend("account_creation_failed");
            var assigned = await userManager.AddToRoleAsync(user, Roles.Candidate);
            var linked = assigned.Succeeded
                ? await userManager.AddLoginAsync(user, new UserLoginInfo(scheme, externalId, scheme))
                : IdentityResult.Failed();
            if (!assigned.Succeeded || !linked.Succeeded) return RedirectToFrontend("account_creation_failed");
            await transaction.CommitAsync(cancellationToken);
        }
        if (user.IsBlocked) return RedirectToFrontend("account_blocked");
        var code = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        db.ExternalAuthTickets.Add(new ExternalAuthTicket
        {
            Id = Guid.NewGuid(),
            CodeHash = Hash(code),
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddMinutes(5)
        });
        await db.SaveChangesAsync(cancellationToken);
        await HttpContext.SignOutAsync("External");
        return RedirectToFrontend(null, code);
    }

    [HttpPost("external/exchange")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Exchange(ExchangeRequest request, CancellationToken cancellationToken)
    {
        var hash = Hash(request.Code);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var ticket = await db.ExternalAuthTickets.FirstOrDefaultAsync(item => item.CodeHash == hash, cancellationToken);
        if (ticket is null || ticket.ExpiresAt <= DateTime.UtcNow)
            return Unauthorized(new { message = "Sign-in link has expired or was already used." });
        var deleted = await db.ExternalAuthTickets.Where(item => item.Id == ticket.Id && item.ExpiresAt > DateTime.UtcNow)
            .ExecuteDeleteAsync(cancellationToken);
        if (deleted != 1) return Unauthorized(new { message = "Sign-in link has expired or was already used." });
        var user = await userManager.FindByIdAsync(ticket.UserId.ToString());
        if (user is null || user.IsBlocked) return Unauthorized(new { message = "Account is unavailable." });
        await transaction.CommitAsync(cancellationToken);
        var roles = await userManager.GetRolesAsync(user);
        return Ok(new AuthResponse(await tokenService.CreateAsync(user),
            new CurrentUserResponse(user.Id, user.Email ?? "", user.FirstName, user.LastName, roles)));
    }

    private bool Configured(string scheme) =>
        !string.IsNullOrWhiteSpace(configuration[$"Authentication:{scheme}:ClientId"])
        && !string.IsNullOrWhiteSpace(configuration[$"Authentication:{scheme}:ClientSecret"]);

    private static string? Scheme(string provider) => provider.ToLowerInvariant() switch
    {
        "google" => "Google",
        "github" => "GitHub",
        _ => null
    };

    private IActionResult RedirectToFrontend(string? error, string? code = null)
    {
        var origin = configuration["Frontend:BaseUrl"]?.TrimEnd('/') ?? "http://127.0.0.1:5173";
        var query = error is null ? $"code={Uri.EscapeDataString(code!)}" : $"error={Uri.EscapeDataString(error)}";
        return Redirect($"{origin}/auth/callback?{query}");
    }

    private static string Hash(string code) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(code)));
}

public sealed record ExchangeRequest([Required] string Code);
