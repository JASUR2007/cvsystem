using backend.Auth;
using backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

[ApiController]
[Authorize]
[Route("api/cvs/{cvId:guid}/likes")]
public sealed class LikesController(ILikeService likeService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(Guid cvId, CancellationToken cancellationToken)
    {
        var result = await likeService.GetLikesAsync(cvId, cancellationToken);
        return Ok(result);
    }

    [Authorize(Roles = Roles.Recruiter + "," + Roles.Administrator)]
    [HttpPost]
    public async Task<IActionResult> Add(Guid cvId, CancellationToken cancellationToken)
    {
        var result = await likeService.AddLikeAsync(cvId, cancellationToken);
        return Ok(result);
    }

    [Authorize(Roles = Roles.Recruiter + "," + Roles.Administrator)]
    [HttpDelete]
    public async Task<IActionResult> Remove(Guid cvId, CancellationToken cancellationToken)
    {
        await likeService.RemoveLikeAsync(cvId, cancellationToken);
        return NoContent();
    }
}
