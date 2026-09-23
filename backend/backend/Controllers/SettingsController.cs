using backend.Auth;
using backend.DTOs.Settings;
using backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

[ApiController]
[Authorize]
[Route("api/settings")]
public sealed class SettingsController(ISettingsService settingsService, ICurrentUserService currentUser) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<SettingsView>> Get(CancellationToken cancellationToken)
    {
        var result = await settingsService.GetSettingsAsync(currentUser.RequireUserId(), cancellationToken);
        return Ok(result);
    }

    [HttpPut("language")]
    public async Task<ActionResult<SettingsView>> Language(SettingsUpdate request, CancellationToken cancellationToken)
    {
        var result = await settingsService.UpdateLanguageAsync(currentUser.RequireUserId(), request, cancellationToken);
        return Ok(result);
    }

    [HttpPut("theme")]
    public async Task<ActionResult<SettingsView>> Theme(SettingsUpdate request, CancellationToken cancellationToken)
    {
        var result = await settingsService.UpdateThemeAsync(currentUser.RequireUserId(), request, cancellationToken);
        return Ok(result);
    }
}
