using backend.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers;

[ApiController]
[Route("api/home")]
public sealed class HomeController(AppDbContext db) : ControllerBase
{
    [HttpGet("latest-positions")]
    public async Task<IActionResult> Latest(CancellationToken cancellationToken) => Ok(await db.Positions.AsNoTracking()
        .Where(position => position.IsPublic).OrderByDescending(position => position.UpdatedAt).Take(5)
        .Select(position => new PositionListItem(position.Id, position.Title, position.Company, position.Level, position.UpdatedAt,
            position.IsPublic, position.Cvs.Count(cv => cv.Status == CvStatus.Published))).ToListAsync(cancellationToken));

    [HttpGet("popular-positions")]
    public async Task<IActionResult> Popular(CancellationToken cancellationToken) => Ok(await db.Positions.AsNoTracking()
        .Where(position => position.IsPublic)
        .OrderByDescending(position => position.Cvs.Count(cv => cv.Status == CvStatus.Published)).ThenBy(position => position.Title).Take(5)
        .Select(position => new { position.Id, position.Title, CvCount = position.Cvs.Count(cv => cv.Status == CvStatus.Published) })
        .ToListAsync(cancellationToken));

    [HttpGet("statistics")]
    public async Task<IActionResult> Statistics(CancellationToken cancellationToken)
    {
        var users = await db.Users.CountAsync(cancellationToken);
        var candidates = await db.UserRoles.CountAsync(role => db.Roles.Any(item => item.Id == role.RoleId && item.Name == "Candidate"), cancellationToken);
        var recruiters = await db.UserRoles.CountAsync(role => db.Roles.Any(item => item.Id == role.RoleId && item.Name == "Recruiter"), cancellationToken);
        var positions = await db.Positions.CountAsync(cancellationToken);
        var publishedCvs = await db.Cvs.CountAsync(cv => cv.Status == CvStatus.Published, cancellationToken);
        var newCvs = await db.Cvs.CountAsync(cv => cv.CreatedAt >= DateTime.UtcNow.AddDays(-1), cancellationToken);
        return Ok(new { users, candidates, recruiters, positions, publishedCvs, newCvs });
    }

    [HttpGet("tags")]
    public async Task<IActionResult> Tags(CancellationToken cancellationToken) => Ok(await db.Tags.AsNoTracking()
        .OrderByDescending(tag => tag.Projects.Count).Take(20)
        .Select(tag => new { tag.Name, Count = tag.Projects.Count }).ToListAsync(cancellationToken));
}
