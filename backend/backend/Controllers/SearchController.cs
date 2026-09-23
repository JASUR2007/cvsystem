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
public sealed class SearchController(ISearchService searchService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Search(string q, CancellationToken cancellationToken)
    {
        var result = await searchService.SearchAllAsync(q, cancellationToken);
        return Ok(result);
    }

    [HttpGet("positions")]
    public async Task<ActionResult<PagedResult<PositionListItem>>> Positions(
        string q, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var result = await searchService.SearchPositionsAsync(q, page, pageSize, cancellationToken);
        return Ok(result);
    }

    [Authorize(Roles = Roles.Recruiter + "," + Roles.Administrator)]
    [HttpGet("cvs")]
    public async Task<ActionResult<PagedResult<SearchCvItem>>> Cvs(
        string q, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var result = await searchService.SearchCvsAsync(q, page, pageSize, cancellationToken);
        return Ok(result);
    }
}
