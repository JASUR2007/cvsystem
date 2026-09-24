using backend.Auth;
using backend.Common.Enums;
using backend.DTOs.Cvs;
using backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

[ApiController]
[Authorize(Roles = Roles.Recruiter + "," + Roles.Administrator)]
[Route("api/positions/{positionId:guid}/cvs")]
public sealed class PositionCvsController(IPositionService positionService) : ControllerBase {
    [HttpGet]
    public async Task<ActionResult<PositionCvPage>> List(
        Guid positionId, string? q, Guid? attributeId, AccessOperator? operation, string? value,
        string? sort = null, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default) {
        var result = await positionService.ListPositionCvsAsync(
            positionId, q, attributeId, operation, value, sort, page, pageSize, cancellationToken);
        return Ok(result);
    }
}
