using backend.Auth;
using backend.Common.Exceptions;
using backend.DTOs.Profile;
using backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

[ApiController]
[Authorize]
[Route("api/profile")]
public sealed class ProfileController(IProfileService profileService, ICurrentUserService currentUser) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ProfileView>> Get(Guid? userId, CancellationToken cancellationToken)
    {
        var targetId = ResolveUserId(userId);
        var result = await profileService.GetProfileAsync(targetId, cancellationToken);
        return Ok(result);
    }

    [HttpPut]
    public async Task<ActionResult<ProfileView>> Update(ProfileUpdate request, Guid? userId, CancellationToken cancellationToken)
    {
        var targetId = ResolveUserId(userId);
        var result = await profileService.UpdateProfileAsync(targetId, request, cancellationToken);
        return Ok(result);
    }

    [Authorize(Roles = Roles.Candidate + "," + Roles.Administrator)]
    [HttpGet("attributes")]
    public async Task<ActionResult<List<AttributeValueView>>> Attributes(Guid? userId, CancellationToken cancellationToken)
    {
        var targetId = ResolveUserId(userId);
        var result = await profileService.GetProfileAttributesAsync(targetId, cancellationToken);
        return Ok(result);
    }

    [Authorize(Roles = Roles.Candidate + "," + Roles.Administrator)]
    [HttpPost("attributes/{attributeId:guid}")]
    public async Task<ActionResult<AttributeValueView>> AddAttribute(Guid attributeId, Guid? userId, CancellationToken cancellationToken)
    {
        var targetId = ResolveUserId(userId);
        var result = await profileService.AddProfileAttributeAsync(targetId, attributeId, cancellationToken);
        return Ok(result);
    }

    [Authorize(Roles = Roles.Candidate + "," + Roles.Administrator)]
    [HttpPut("attributes/{attributeId:guid}")]
    public async Task<ActionResult<AttributeValueView>> UpdateAttribute(Guid attributeId, AttributeValueUpdate request, Guid? userId, CancellationToken cancellationToken)
    {
        var targetId = ResolveUserId(userId);
        var result = await profileService.UpdateProfileAttributeAsync(targetId, attributeId, request, cancellationToken);
        return Ok(result);
    }

    [Authorize(Roles = Roles.Candidate + "," + Roles.Administrator)]
    [HttpDelete("attributes/{attributeId:guid}")]
    public async Task<IActionResult> RemoveAttribute(Guid attributeId, Guid? userId, CancellationToken cancellationToken)
    {
        var targetId = ResolveUserId(userId);
        await profileService.RemoveProfileAttributeAsync(targetId, attributeId, cancellationToken);
        return NoContent();
    }

    private Guid ResolveUserId(Guid? target)
    {
        var current = currentUser.RequireUserId();
        if (target is null || target == current) return current;
        if (currentUser.IsAdmin) return target.Value;
        throw new ForbiddenException();
    }
}
