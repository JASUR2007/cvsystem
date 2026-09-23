using backend.Api;
using backend.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers;

[ApiController]
[Authorize]
[Route("api/settings")]
public sealed class SettingsController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var id = ApiModels.UserId(User);
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        return user is null ? NotFound() : Ok(new SettingsView(user.Language, user.Theme, user.Version));
    }

    [HttpPut("language")]
    public Task<IActionResult> Language(SettingsUpdate request, CancellationToken cancellationToken) =>
        Update(request, true, cancellationToken);

    [HttpPut("theme")]
    public Task<IActionResult> Theme(SettingsUpdate request, CancellationToken cancellationToken) =>
        Update(request, false, cancellationToken);

    private async Task<IActionResult> Update(SettingsUpdate request, bool language, CancellationToken cancellationToken)
    {
        if (language && request.Value is not ("en" or "ru" or "uz")) return BadRequest(new { message = "Unsupported language." });
        if (!language && request.Value is not ("light" or "dark")) return BadRequest(new { message = "Unsupported theme." });
        var id = ApiModels.UserId(User);
        var user = await db.Users.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (user is null) return NotFound();
        if (user.Version != request.Version) return Conflict(new { message = "Settings were changed in another session." });
        if (language) user.Language = request.Value;
        else user.Theme = request.Value;
        user.Version++;
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new { message = "Settings were changed in another session." });
        }
        return Ok(new SettingsView(user.Language, user.Theme, user.Version));
    }
}

public sealed record SettingsUpdate(string Value, int Version);
public sealed record SettingsView(string Language, string Theme, int Version);
