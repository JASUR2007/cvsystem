using backend.Api;
using backend.Auth;
using backend.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers;

[ApiController]
[Authorize]
[Route("api/profile")]
public sealed class ProfileController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(Guid? userId, CancellationToken cancellationToken)
    {
        var id = ResolveUserId(userId);
        if (id is null) return Forbid();
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        return user is null ? NotFound() : Ok(ToView(user));
    }

    [HttpPut]
    public async Task<IActionResult> Update(ProfileUpdate request, Guid? userId, CancellationToken cancellationToken)
    {
        var id = ResolveUserId(userId);
        if (id is null) return Forbid();
        var user = await db.Users.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (user is null) return NotFound();
        if (user.Version != request.Version) return Conflict(new { message = "Profile was changed in another session." });
        if (string.IsNullOrWhiteSpace(request.FirstName) || string.IsNullOrWhiteSpace(request.LastName)
            || request.FirstName.Length > 100 || request.LastName.Length > 100 || request.Location?.Length > 200 || request.PhotoObjectKey?.Length > 500)
            return BadRequest(new { message = "Invalid profile fields." });
        if (!ApiModels.ValidImageKey(request.PhotoObjectKey, id.Value))
            return BadRequest(new { message = "Photo must belong to this profile." });

        user.FirstName = request.FirstName.Trim();
        user.LastName = request.LastName.Trim();
        user.Location = request.Location?.Trim();
        user.PhotoObjectKey = request.PhotoObjectKey?.Trim();
        user.Version++;
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new { message = "Profile was changed in another session." });
        }

        return Ok(ToView(user));
    }

    [Authorize(Roles = Roles.Candidate + "," + Roles.Administrator)]
    [HttpGet("attributes")]
    public async Task<IActionResult> Attributes(Guid? userId, CancellationToken cancellationToken)
    {
        var id = ResolveUserId(userId);
        if (id is null) return Forbid();
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (user is null) return NotFound();

        var values = await db.UserAttributeValues.AsNoTracking()
            .Where(value => value.UserId == id)
            .Include(value => value.Attribute)
            .ToListAsync(cancellationToken);
        var builtIns = await db.Attributes.AsNoTracking().Where(attribute => attribute.IsBuiltIn).ToListAsync(cancellationToken);
        var result = builtIns.Select(attribute => AttributeValues.View(attribute, null, user))
            .Concat(values.Select(value => AttributeValues.View(value.Attribute, value, user)))
            .OrderBy(value => value.Category).ThenBy(value => value.Name).ToList();
        return Ok(result);
    }

    [Authorize(Roles = Roles.Candidate + "," + Roles.Administrator)]
    [HttpPost("attributes/{attributeId:guid}")]
    public async Task<IActionResult> AddAttribute(Guid attributeId, Guid? userId, CancellationToken cancellationToken)
    {
        var id = ResolveUserId(userId);
        if (id is null) return Forbid();
        var attribute = await db.Attributes.FindAsync([attributeId], cancellationToken);
        if (attribute is null) return NotFound();
        if (attribute.IsBuiltIn) return BadRequest(new { message = "Built-in attributes already belong to the profile." });
        if (await db.UserAttributeValues.AnyAsync(value => value.UserId == id && value.AttributeId == attributeId, cancellationToken))
            return Conflict(new { message = "Attribute already added." });

        var user = await db.Users.FindAsync([id], cancellationToken);
        if (user is null) return NotFound();
        var value = new UserAttributeValue { UserId = id.Value, AttributeId = attributeId };
        db.UserAttributeValues.Add(value);
        await db.SaveChangesAsync(cancellationToken);
        return Ok(AttributeValues.View(attribute, value, user));
    }

    [Authorize(Roles = Roles.Candidate + "," + Roles.Administrator)]
    [HttpPut("attributes/{attributeId:guid}")]
    public async Task<IActionResult> UpdateAttribute(Guid attributeId, AttributeValueUpdate request, Guid? userId, CancellationToken cancellationToken)
    {
        var id = ResolveUserId(userId);
        if (id is null) return Forbid();
        var value = await db.UserAttributeValues.Include(item => item.Attribute).ThenInclude(attribute => attribute.Options)
            .FirstOrDefaultAsync(item => item.UserId == id && item.AttributeId == attributeId, cancellationToken);
        if (value is null) return NotFound();
        if (value.Version != request.Version) return Conflict(new { message = "Attribute value was changed in another session." });
        var error = AttributeValues.Validate(value.Attribute, request.Value);
        if (error is not null) return BadRequest(new { message = error });
        if (!ApiModels.ValidImageKey(request.Value.ImageObjectKey, id.Value))
            return BadRequest(new { message = "Image must belong to this profile." });

        AttributeValues.Apply(value, value.Attribute, request.Value);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new { message = "Attribute value was changed in another session." });
        }

        var user = await db.Users.AsNoTracking().FirstAsync(item => item.Id == id, cancellationToken);
        return Ok(AttributeValues.View(value.Attribute, value, user));
    }

    [Authorize(Roles = Roles.Candidate + "," + Roles.Administrator)]
    [HttpDelete("attributes/{attributeId:guid}")]
    public async Task<IActionResult> RemoveAttribute(Guid attributeId, Guid? userId, CancellationToken cancellationToken)
    {
        var id = ResolveUserId(userId);
        if (id is null) return Forbid();
        var value = await db.UserAttributeValues.FindAsync([id.Value, attributeId], cancellationToken);
        if (value is null) return NotFound();
        if (await db.Cvs.AnyAsync(cv => cv.CandidateId == id && cv.Position.Attributes.Any(attribute => attribute.AttributeId == attributeId), cancellationToken))
            return Conflict(new { message = "Attribute is required by an existing CV." });
        db.UserAttributeValues.Remove(value);
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private Guid? ResolveUserId(Guid? target)
    {
        var current = ApiModels.UserId(User);
        return target is null || target == current ? current : ApiModels.IsAdmin(User) ? target : null;
    }

    private static ProfileView ToView(AppUser user) => new(user.Id, user.FirstName, user.LastName, user.Email ?? string.Empty,
        user.Location, user.PhotoObjectKey, user.Language, user.Theme, user.Version);
}

public sealed record ProfileUpdate(string FirstName, string LastName, string? Location, string? PhotoObjectKey, int Version);
public sealed record ProfileView(Guid Id, string FirstName, string LastName, string Email, string? Location, string? PhotoObjectKey, string Language, string Theme, int Version);
public sealed record AttributeValueUpdate(int Version, AttributeValueInput Value);
