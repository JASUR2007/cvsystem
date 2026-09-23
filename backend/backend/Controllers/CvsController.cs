using backend.Auth;
using backend.Common.Exceptions;
using backend.DTOs.Cvs;
using backend.DTOs.Profile;
using backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

[ApiController]
[Authorize]
public sealed class CvsController(ICvService cvService, ICurrentUserService currentUser) : ControllerBase
{
    [HttpGet("api/profile/cvs")]
    [HttpGet("api/users/{userId:guid}/cvs")]
    public async Task<ActionResult<List<CvListItem>>> List(Guid? userId, CancellationToken cancellationToken)
    {
        var targetId = ResolveUserId(userId);
        var result = await cvService.ListCandidateCvsAsync(targetId, cancellationToken);
        return Ok(result);
    }

    [Authorize(Roles = Roles.Candidate + "," + Roles.Administrator)]
    [HttpPost("api/positions/{positionId:guid}/cvs")]
    public async Task<ActionResult<CvDetail>> Create(Guid positionId, CancellationToken cancellationToken)
    {
        var result = await cvService.CreateCvAsync(positionId, currentUser.RequireUserId(), cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }

    [HttpGet("api/cvs/{id:guid}")]
    public async Task<ActionResult<CvDetail>> Get(Guid id, CancellationToken cancellationToken)
    {
        var result = await cvService.GetCvDetailAsync(id, cancellationToken);
        return Ok(result);
    }

    [Authorize(Roles = Roles.Candidate + "," + Roles.Administrator)]
    [HttpPut("api/cvs/{id:guid}/attributes/{attributeId:guid}")]
    public async Task<ActionResult<AttributeValueView>> UpdateAttribute(
        Guid id, Guid attributeId, AttributeValueUpdate request, CancellationToken cancellationToken)
    {
        var result = await cvService.UpdateCvAttributeAsync(id, attributeId, request, cancellationToken);
        return Ok(result);
    }

    [Authorize(Roles = Roles.Candidate + "," + Roles.Administrator)]
    [HttpPost("api/cvs/{id:guid}/publish")]
    public async Task<IActionResult> Publish(Guid id, CancellationToken cancellationToken)
    {
        var result = await cvService.PublishCvAsync(id, cancellationToken);
        return Ok(result);
    }

    [Authorize(Roles = Roles.Candidate + "," + Roles.Administrator)]
    [HttpDelete("api/cvs/{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await cvService.DeleteCvAsync(id, cancellationToken);
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
