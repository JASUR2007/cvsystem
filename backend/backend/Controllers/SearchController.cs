using backend.Api;
using backend.Auth;
using backend.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers;

[ApiController]
[Route("api/search")]
public sealed class SearchController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Search(string q, CancellationToken cancellationToken)
    {
        var positions = await PositionResults(q, 1, 10, cancellationToken);
        if (!User.IsInRole(Roles.Recruiter) && !ApiModels.IsAdmin(User)) return Ok(new { positions, cvs = new PageResult<SearchCvItem>([], 1, 10, 0) });
        var cvs = await CvResults(q, 1, 10, cancellationToken);
        return Ok(new { positions, cvs });
    }

    [HttpGet("positions")]
    public async Task<IActionResult> Positions(string q, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default) =>
        Ok(await PositionResults(q, page, pageSize, cancellationToken));

    [Authorize(Roles = Roles.Recruiter + "," + Roles.Administrator)]
    [HttpGet("cvs")]
    public async Task<IActionResult> Cvs(string q, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default) =>
        Ok(await CvResults(q, page, pageSize, cancellationToken));

    private async Task<PageResult<PositionListItem>> PositionResults(string q, int page, int pageSize, CancellationToken cancellationToken)
    {
        var query = db.Positions.AsNoTracking().AsQueryable();
        if (!User.Identity?.IsAuthenticated ?? true) query = query.Where(position => position.IsPublic);
        else if (User.IsInRole(Roles.Candidate) && !ApiModels.IsAdmin(User))
            query = PositionAccess.Eligible(query, db, ApiModels.UserId(User)!.Value);
        if (!string.IsNullOrWhiteSpace(q)) query = query.Where(position => position.SearchVector.Matches(q.Trim()));
        var currentPage = ApiModels.Page(page);
        var size = ApiModels.PageSize(pageSize);
        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(position => position.UpdatedAt).Skip((currentPage - 1) * size).Take(size)
            .Select(position => new PositionListItem(position.Id, position.Title, position.Company, position.Level,
                position.UpdatedAt, position.IsPublic, position.Cvs.Count(cv => cv.Status == CvStatus.Published)))
            .ToListAsync(cancellationToken);
        return new PageResult<PositionListItem>(items, currentPage, size, total);
    }

    private async Task<PageResult<SearchCvItem>> CvResults(string q, int page, int pageSize, CancellationToken cancellationToken)
    {
        var query = db.Cvs.AsNoTracking().Where(cv => cv.Status == CvStatus.Published);
        if (!ApiModels.IsAdmin(User)) query = PositionAccess.EligibleCvs(query, db);
        if (!string.IsNullOrWhiteSpace(q)) query = query.Where(cv => cv.Position.SearchVector.Matches(q.Trim()) || cv.Candidate.SearchVector.Matches(q.Trim()));
        var currentPage = ApiModels.Page(page);
        var size = ApiModels.PageSize(pageSize);
        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(cv => cv.UpdatedAt).Skip((currentPage - 1) * size).Take(size)
            .Select(cv => new SearchCvItem(cv.Id, cv.PositionId, cv.Position.Title, cv.Candidate.FirstName + " " + cv.Candidate.LastName,
                cv.UpdatedAt, cv.Likes.Count)).ToListAsync(cancellationToken);
        return new PageResult<SearchCvItem>(items, currentPage, size, total);
    }
}

public sealed record SearchCvItem(Guid Id, Guid PositionId, string Position, string Candidate, DateTime UpdatedAt, int Likes);
