using backend.Api;
using backend.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers;

[ApiController]
[Authorize]
public sealed class DiscussionController(AppDbContext db) : ControllerBase
{
    [HttpGet("api/positions/{positionId:guid}/discussion")]
    public async Task<IActionResult> List(Guid positionId, CancellationToken cancellationToken)
    {
        if (!await CanAccess(positionId, cancellationToken)) return NotFound();
        var posts = await db.DiscussionPosts.AsNoTracking().Where(post => post.PositionId == positionId)
            .OrderBy(post => post.CreatedAt).ThenBy(post => post.Id)
            .Select(post => new DiscussionView(post.Id, post.AuthorId, post.Author.FirstName + " " + post.Author.LastName,
                post.Content, post.CreatedAt, post.UpdatedAt)).ToListAsync(cancellationToken);
        return Ok(posts);
    }

    [HttpPost("api/positions/{positionId:guid}/discussion")]
    public async Task<IActionResult> Add(Guid positionId, DiscussionRequest request, CancellationToken cancellationToken)
    {
        if (!await CanAccess(positionId, cancellationToken)) return NotFound();
        if (string.IsNullOrWhiteSpace(request.Content) || request.Content.Length > 10000) return BadRequest(new { message = "Post must contain 1–10000 characters." });
        var authorId = ApiModels.UserId(User);
        if (authorId is null) return Unauthorized();
        var post = new DiscussionPost { Id = Guid.NewGuid(), PositionId = positionId, AuthorId = authorId.Value, Content = request.Content.Trim() };
        db.DiscussionPosts.Add(post);
        await db.SaveChangesAsync(cancellationToken);
        var author = await db.Users.AsNoTracking().FirstAsync(user => user.Id == authorId, cancellationToken);
        return Ok(new DiscussionView(post.Id, authorId.Value, author.FirstName + " " + author.LastName, post.Content, post.CreatedAt, null));
    }

    [HttpPut("api/discussions/{id:guid}")]
    public async Task<IActionResult> Update(Guid id, DiscussionRequest request, CancellationToken cancellationToken)
    {
        var post = await db.DiscussionPosts.Include(item => item.Author).FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (post is null) return NotFound();
        if (post.AuthorId != ApiModels.UserId(User) && !ApiModels.IsAdmin(User)) return Forbid();
        if (string.IsNullOrWhiteSpace(request.Content) || request.Content.Length > 10000) return BadRequest(new { message = "Post must contain 1–10000 characters." });
        post.Content = request.Content.Trim();
        post.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new DiscussionView(post.Id, post.AuthorId, post.Author.FirstName + " " + post.Author.LastName, post.Content, post.CreatedAt, post.UpdatedAt));
    }

    [HttpDelete("api/discussions/{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var post = await db.DiscussionPosts.FindAsync([id], cancellationToken);
        if (post is null) return NotFound();
        if (post.AuthorId != ApiModels.UserId(User) && !ApiModels.IsAdmin(User)) return Forbid();
        db.DiscussionPosts.Remove(post);
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private async Task<bool> CanAccess(Guid positionId, CancellationToken cancellationToken)
    {
        var positions = db.Positions.AsNoTracking().Where(position => position.Id == positionId);
        var userId = ApiModels.UserId(User);
        if (userId is null) return false;
        if (!User.IsInRole("Recruiter") && !ApiModels.IsAdmin(User))
            positions = PositionAccess.Eligible(positions, db, userId.Value);
        return await positions.AnyAsync(cancellationToken);
    }
}

public sealed record DiscussionRequest(string Content);
public sealed record DiscussionView(Guid Id, Guid AuthorId, string Author, string Content, DateTime CreatedAt, DateTime? UpdatedAt);
