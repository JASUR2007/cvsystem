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
public sealed class ProfileController(IProfileService profileService, ICurrentUserService currentUser) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ProfileView>> Get(Guid? userId, CancellationToken cancellationToken)
    {
        var targetId = ResolveUserId(userId);
        var result = await profileService.GetProfileAsync(targetId, cancellationToken);
        return Ok(result);
    }

    [HttpPost("request-recruiter")]
    public async Task<ActionResult<object>> RequestRecruiter(
        [FromServices] UserManager<AppUser> userManager,
        [FromServices] JwtTokenService jwtTokenService,
        CancellationToken cancellationToken)
    {
        var id = currentUser.RequireUserId();
        var user = await userManager.FindByIdAsync(id.ToString());
        if (user is null) throw new NotFoundException("User not found.");

        if (!await userManager.IsInRoleAsync(user, Roles.Recruiter))
        {
            var addResult = await userManager.AddToRoleAsync(user, Roles.Recruiter);
            if (!addResult.Succeeded)
            {
                throw new ValidationException(string.Join(" ", addResult.Errors.Select(e => e.Description)));
            }
        }

        var roles = await userManager.GetRolesAsync(user);
        var newToken = await jwtTokenService.CreateAsync(user);

        Response.Cookies.Append("talenthub_token", newToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Expires = DateTimeOffset.UtcNow.AddHours(1)
        });

        return Ok(new
        {
            token = newToken,
            user = new
            {
                id = user.Id,
                email = user.Email,
                firstName = user.FirstName,
                lastName = user.LastName,
                roles = roles.ToList()
            }
        });
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
