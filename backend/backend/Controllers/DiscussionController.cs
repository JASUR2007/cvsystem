using backend.DTOs.Discussions;
using backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

[ApiController]
[Authorize]
public sealed class DiscussionController(IDiscussionService discussionService) : ControllerBase {
    [HttpGet("api/positions/{positionId:guid}/discussion")]
    public async Task<ActionResult<List<DiscussionView>>> List(Guid positionId, CancellationToken cancellationToken) {
        var result = await discussionService.ListPostsAsync(positionId, cancellationToken);
        return Ok(result);
    }

    [HttpPost("api/positions/{positionId:guid}/discussion")]
    public async Task<ActionResult<DiscussionView>> Add(Guid positionId, DiscussionRequest request, CancellationToken cancellationToken) {
        var result = await discussionService.AddPostAsync(positionId, request, cancellationToken);
        return Ok(result);
    }

    [HttpPut("api/discussions/{id:guid}")]
    public async Task<ActionResult<DiscussionView>> Update(Guid id, DiscussionRequest request, CancellationToken cancellationToken) {
        var result = await discussionService.UpdatePostAsync(id, request, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("api/discussions/{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken) {
        await discussionService.DeletePostAsync(id, cancellationToken);
        return NoContent();
    }
}
