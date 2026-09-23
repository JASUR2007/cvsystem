using backend.Auth;
using backend.Common.Enums;
using backend.Common.Pagination;
using backend.DTOs.Positions;
using backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

[ApiController]
[Route("api/positions")]
public sealed class PositionsController(IPositionService positionService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<PositionListItem>>> List(
        PositionLevel? level, string? q, bool? isPublic, int page = 1, int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await positionService.ListPositionsAsync(level, q, isPublic, page, pageSize, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PositionDetail>> Get(Guid id, CancellationToken cancellationToken)
    {
        var result = await positionService.GetPositionByIdAsync(id, cancellationToken);
        return Ok(result);
    }

    [Authorize(Roles = Roles.Recruiter + "," + Roles.Administrator)]
    [HttpPost]
    public async Task<ActionResult<PositionDetail>> Create(PositionRequest request, CancellationToken cancellationToken)
    {
        var result = await positionService.CreatePositionAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }

    [Authorize(Roles = Roles.Recruiter + "," + Roles.Administrator)]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<PositionDetail>> Update(Guid id, PositionRequest request, CancellationToken cancellationToken)
    {
        var result = await positionService.UpdatePositionAsync(id, request, cancellationToken);
        return Ok(result);
    }

    [Authorize(Roles = Roles.Recruiter + "," + Roles.Administrator)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await positionService.DeletePositionAsync(id, cancellationToken);
        return NoContent();
    }

    [Authorize(Roles = Roles.Recruiter + "," + Roles.Administrator)]
    [HttpPost("{id:guid}/duplicate")]
    public async Task<ActionResult<PositionDetail>> Duplicate(Guid id, CancellationToken cancellationToken)
    {
        var result = await positionService.DuplicatePositionAsync(id, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }
}
