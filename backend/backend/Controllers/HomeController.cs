using backend.Data;
using backend.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HomeController : ControllerBase
{
    private readonly AppDbContext _db;

    public HomeController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet("statistics")]
    public async Task<IActionResult> GetStatistics()
    {
        var users = await _db.Users.CountAsync();
        var candidates = await _db.UserRoles.Join(_db.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => r).Where(r => r.Name == Roles.Candidate).CountAsync();
        var recruiters = await _db.UserRoles.Join(_db.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => r).Where(r => r.Name == Roles.Recruiter).CountAsync();
        var positions = await _db.Positions.CountAsync();
        var publishedCvs = await _db.Cvs.CountAsync();
        var cvsLast24Hours = await _db.Cvs.Where(c => c.CreatedAt >= DateTime.UtcNow.AddDays(-1)).CountAsync();

        return Ok(new
        {
            users,
            candidates,
            recruiters,
            positions,
            publishedCvs,
            cvsLast24Hours
        });
    }

    [HttpGet("latest-positions")]
    public async Task<IActionResult> GetLatestPositions()
    {
        var positions = await _db.Positions
            .OrderByDescending(p => p.UpdatedAt)
            .Take(10)
            .Select(p => new { p.Id, p.Title, p.Company, p.Level, p.UpdatedAt })
            .ToListAsync();
        return Ok(positions);
    }

    [HttpGet("popular-positions")]
    public async Task<IActionResult> GetPopularPositions()
    {
        var positions = await _db.Positions
            .OrderByDescending(p => p.Cvs.Count)
            .Take(5)
            .Select(p => new { p.Id, p.Title, cvCount = p.Cvs.Count })
            .ToListAsync();
        return Ok(positions);
    }

    [HttpGet("tags")]
    public async Task<IActionResult> GetTags()
    {
        var tags = await _db.Tags
            .OrderByDescending(t => t.Projects.Count)
            .Take(20)
            .Select(t => new { t.Name, count = t.Projects.Count })
            .ToListAsync();
        return Ok(tags);
    }
}
