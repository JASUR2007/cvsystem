using backend.DTOs.Auth;
using backend.Services.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class ExternalAuthController(
    IAuthService authService,
    IConfiguration configuration,
    IAuthenticationSchemeProvider schemes) : ControllerBase
{
    [HttpGet("providers")]
    public IActionResult Providers() => Ok(authService.GetExternalProvidersStatus());

    [HttpGet("external/{provider}")]
    public async Task<IActionResult> Start(string provider)
    {
        var scheme = Scheme(provider);
        if (scheme is null || await schemes.GetSchemeAsync(scheme) is null)
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
        if (scheme is null) return RedirectToFrontend("provider_unavailable");

        var authentication = await HttpContext.AuthenticateAsync("External");
        if (!authentication.Succeeded || authentication.Principal is null)
            return RedirectToFrontend("authentication_failed");

        if (authentication.Properties is null
            || !authentication.Properties.Items.TryGetValue("provider", out var originalProvider)
            || originalProvider != scheme)
            return RedirectToFrontend("authentication_failed");

        try
        {
            var code = await authService.ProcessExternalLoginAsync(scheme, authentication.Principal, cancellationToken);
            await HttpContext.SignOutAsync("External");
            return RedirectToFrontend(null, code);
        }
        catch (Exception ex)
        {
            await HttpContext.SignOutAsync("External");
            return RedirectToFrontend(ex.Message);
        }
    }

    [HttpPost("external/exchange")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Exchange(ExchangeRequest request, CancellationToken cancellationToken)
    {
        var response = await authService.ExchangeExternalCodeAsync(request.Code, cancellationToken);
        return Ok(response);
    }

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
}
