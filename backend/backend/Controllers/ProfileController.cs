using backend.Auth;
using backend.Common.Exceptions;
using backend.DTOs.Profile;
using backend.Entities;
using backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

[ApiController]
[Authorize]
[Route("api/profile")]
public sealed class ProfileController(IProfileService profileService, ICurrentUserService currentUser) : ControllerBase {
    [HttpGet]
    public async Task<ActionResult<ProfileView>> Get(Guid? userId, CancellationToken cancellationToken) {
        var targetId = ResolveUserId(userId);
        var result = await profileService.GetProfileAsync(targetId, cancellationToken);
        return Ok(result);
    }

    [HttpPost("request-recruiter")]
    public async Task<ActionResult<object>> RequestRecruiter(
        [FromServices] UserManager<AppUser> userManager,
        CancellationToken cancellationToken) {
        var id = currentUser.RequireUserId();
        var user = await userManager.FindByIdAsync(id.ToString());
        if (user is null) throw new NotFoundException("User not found.");

        if (await userManager.IsInRoleAsync(user, Roles.Recruiter)) {
            return Ok(new { status = "Approved", message = "Вы уже являетесь рекрутером." });
        }

        var existingClaim = (await userManager.GetClaimsAsync(user))
            .FirstOrDefault(c => c.Type == "recruiter_request_status");
        if (existingClaim is not null) {
            await userManager.ReplaceClaimAsync(user, existingClaim, new System.Security.Claims.Claim("recruiter_request_status", "Pending"));
        }
        else {
            await userManager.AddClaimAsync(user, new System.Security.Claims.Claim("recruiter_request_status", "Pending"));
        }

        return Ok(new {
            status = "Pending",
            message = "Запрос отправлен. Ожидание рассмотрения администратором."
        });
    }

    [HttpPut]
    public async Task<ActionResult<ProfileView>> Update(ProfileUpdate request, Guid? userId, CancellationToken cancellationToken) {
        var targetId = ResolveUserId(userId);
        var result = await profileService.UpdateProfileAsync(targetId, request, cancellationToken);
        return Ok(result);
    }

    [Authorize(Roles = Roles.Candidate + "," + Roles.Administrator)]
    [HttpGet("attributes")]
    public async Task<ActionResult<List<AttributeValueView>>> Attributes(Guid? userId, CancellationToken cancellationToken) {
        var targetId = ResolveUserId(userId);
        var result = await profileService.GetProfileAttributesAsync(targetId, cancellationToken);
        return Ok(result);
    }

    [Authorize(Roles = Roles.Candidate + "," + Roles.Administrator)]
    [HttpPost("attributes/{attributeId:guid}")]
    public async Task<ActionResult<AttributeValueView>> AddAttribute(Guid attributeId, Guid? userId, CancellationToken cancellationToken) {
        var targetId = ResolveUserId(userId);
        var result = await profileService.AddProfileAttributeAsync(targetId, attributeId, cancellationToken);
        return Ok(result);
    }

    [Authorize(Roles = Roles.Candidate + "," + Roles.Administrator)]
    [HttpPut("attributes/{attributeId:guid}")]
    public async Task<ActionResult<AttributeValueView>> UpdateAttribute(Guid attributeId, AttributeValueUpdate request, Guid? userId, CancellationToken cancellationToken) {
        var targetId = ResolveUserId(userId);
        var result = await profileService.UpdateProfileAttributeAsync(targetId, attributeId, request, cancellationToken);
        return Ok(result);
    }

    [Authorize(Roles = Roles.Candidate + "," + Roles.Administrator)]
    [HttpDelete("attributes/{attributeId:guid}")]
    public async Task<IActionResult> RemoveAttribute(Guid attributeId, Guid? userId, CancellationToken cancellationToken) {
        var targetId = ResolveUserId(userId);
        await profileService.RemoveProfileAttributeAsync(targetId, attributeId, cancellationToken);
        return NoContent();
    }

    private Guid ResolveUserId(Guid? target) {
        var current = currentUser.RequireUserId();
        if (target is null || target == current) return current;
        if (currentUser.IsAdmin) return target.Value;
        throw new ForbiddenException();
    }
}
