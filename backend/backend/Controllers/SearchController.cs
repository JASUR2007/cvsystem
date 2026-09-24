using backend.Auth;
using backend.Common.Pagination;
using backend.DTOs.Positions;
using backend.DTOs.Search;
using backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

[ApiController]
[Route("api/search")]
public sealed class SearchController(ISearchService searchService) : ControllerBase {
    [HttpGet]
    public async Task<IActionResult> Search([FromQuery] string? q = null, CancellationToken cancellationToken = default) {
        var result = await searchService.SearchAllAsync(q ?? string.Empty, cancellationToken);
        return Ok(result);
    }

    [HttpGet("positions")]
    public async Task<ActionResult<PagedResult<PositionListItem>>> Positions(
        [FromQuery] string? q = null, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default) {
        var result = await searchService.SearchPositionsAsync(q ?? string.Empty, page, pageSize, cancellationToken);
        return Ok(result);
    }

    [Authorize(Roles = Roles.Recruiter + "," + Roles.Administrator)]
    [HttpGet("cvs")]
    public async Task<ActionResult<PagedResult<SearchCvItem>>> Cvs(
        [FromQuery] string? q = null, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default) {
        var result = await searchService.SearchCvsAsync(q ?? string.Empty, page, pageSize, cancellationToken);
        return Ok(result);
    }
}
