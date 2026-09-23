using backend.Auth;
using backend.Common.Enums;
using backend.Common.Exceptions;
using backend.Data;
using backend.Entities;
using backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace backend.Services;

public class LikeService(AppDbContext db, ICurrentUserService currentUser) : ILikeService
{
    public async Task<object> GetLikesAsync(Guid cvId, CancellationToken cancellationToken = default)
    {
        var currentId = currentUser.UserId;
        var visible = currentUser.IsAdmin ? db.Cvs.AsQueryable() : PositionAccessHelper.EligibleCvs(db.Cvs, db);

        if (!await visible.AnyAsync(cv => cv.Id == cvId && (cv.Status == CvStatus.Published || cv.CandidateId == currentId), cancellationToken))
            throw new NotFoundException("CV not found.");

        var count = await db.CvLikes.CountAsync(like => like.CvId == cvId, cancellationToken);
        var liked = await db.CvLikes.AnyAsync(like => like.CvId == cvId && like.RecruiterId == currentId, cancellationToken);

        return new { count, liked };
    }

    public async Task<object> AddLikeAsync(Guid cvId, CancellationToken cancellationToken = default)
    {
        var visible = currentUser.IsAdmin ? db.Cvs.AsQueryable() : PositionAccessHelper.EligibleCvs(db.Cvs, db);
        if (!await visible.AnyAsync(cv => cv.Id == cvId && cv.Status == CvStatus.Published, cancellationToken))
            throw new NotFoundException("CV not found.");

        var currentId = currentUser.RequireUserId();

        if (await db.CvLikes.AnyAsync(like => like.CvId == cvId && like.RecruiterId == currentId, cancellationToken))
            throw new ConflictException("CV already liked.");

        db.CvLikes.Add(new CvLike { CvId = cvId, RecruiterId = currentId });
        await db.SaveChangesAsync(cancellationToken);

        var count = await db.CvLikes.CountAsync(like => like.CvId == cvId, cancellationToken);
        return new { count, liked = true };
    }

    public async Task RemoveLikeAsync(Guid cvId, CancellationToken cancellationToken = default)
    {
        var currentId = currentUser.RequireUserId();
        var like = await db.CvLikes.FindAsync([cvId, currentId], cancellationToken);
        if (like is null)
            throw new NotFoundException("Like not found.");

        db.CvLikes.Remove(like);
        await db.SaveChangesAsync(cancellationToken);
    }
}
