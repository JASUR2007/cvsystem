using backend.Api;
using backend.Auth;
using backend.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers;

[ApiController]
[Authorize]
[Route("api/cvs/{cvId:guid}/likes")]
public sealed class LikesController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(Guid cvId, CancellationToken cancellationToken)
    {
        var currentId = ApiModels.UserId(User);
        var visible = ApiModels.IsAdmin(User) ? db.Cvs.AsQueryable() : PositionAccess.EligibleCvs(db.Cvs, db);
        if (!await visible.AnyAsync(cv => cv.Id == cvId && (cv.Status == CvStatus.Published || cv.CandidateId == currentId), cancellationToken)) return NotFound();
        var count = await db.CvLikes.CountAsync(like => like.CvId == cvId, cancellationToken);
        var liked = await db.CvLikes.AnyAsync(like => like.CvId == cvId && like.RecruiterId == currentId, cancellationToken);
        return Ok(new { count, liked });
    }

    [Authorize(Roles = Roles.Recruiter + "," + Roles.Administrator)]
    [HttpPost]
    public async Task<IActionResult> Add(Guid cvId, CancellationToken cancellationToken)
    {
        var visible = ApiModels.IsAdmin(User) ? db.Cvs.AsQueryable() : PositionAccess.EligibleCvs(db.Cvs, db);
        if (!await visible.AnyAsync(cv => cv.Id == cvId && cv.Status == CvStatus.Published, cancellationToken)) return NotFound();
        var currentId = ApiModels.UserId(User);
        if (currentId is null) return Unauthorized();
        if (await db.CvLikes.AnyAsync(like => like.CvId == cvId && like.RecruiterId == currentId, cancellationToken)) return Conflict(new { message = "CV already liked." });
        db.CvLikes.Add(new CvLike { CvId = cvId, RecruiterId = currentId.Value });
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { count = await db.CvLikes.CountAsync(like => like.CvId == cvId, cancellationToken), liked = true });
    }

    [Authorize(Roles = Roles.Recruiter + "," + Roles.Administrator)]
    [HttpDelete]
    public async Task<IActionResult> Remove(Guid cvId, CancellationToken cancellationToken)
    {
        var currentId = ApiModels.UserId(User);
        var like = await db.CvLikes.FindAsync([cvId, currentId], cancellationToken);
        if (like is null) return NotFound();
        db.CvLikes.Remove(like);
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}
