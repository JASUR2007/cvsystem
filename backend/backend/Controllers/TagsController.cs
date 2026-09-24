using backend.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers;

[ApiController]
[Authorize]
[Route("api/tags")]
public sealed class TagsController(AppDbContext db) : ControllerBase {
    [HttpGet]
    public async Task<ActionResult<List<string>>> Search(string? prefix, CancellationToken cancellationToken) {
        var query = db.Tags.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(prefix))
            query = query.Where(tag => EF.Functions.ILike(tag.Name, prefix.Trim() + "%"));

        var tags = await query.OrderBy(tag => tag.Name).Take(20).Select(tag => tag.Name).ToListAsync(cancellationToken);
        return Ok(tags);
    }

    [HttpGet("popular")]
    [AllowAnonymous]
    public async Task<IActionResult> Popular(CancellationToken cancellationToken) {
        var tags = await db.Tags.AsNoTracking()
            .OrderByDescending(tag => tag.Projects.Count)
            .ThenBy(tag => tag.Name)
            .Take(20)
            .Select(tag => new { tag.Name, Count = tag.Projects.Count })
            .ToListAsync(cancellationToken);

        return Ok(tags);
    }
}
