using backend.Auth;
using backend.Common.Exceptions;
using backend.Data;
using backend.DTOs.Discussions;
using backend.Entities;
using backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace backend.Services;

public class DiscussionService(AppDbContext db, ICurrentUserService currentUser) : IDiscussionService {
    public async Task<List<DiscussionView>> ListPostsAsync(Guid positionId, CancellationToken cancellationToken = default) {
        await EnsureAccessAsync(positionId, cancellationToken);

        var posts = await db.DiscussionPosts.AsNoTracking().Where(post => post.PositionId == positionId)
            .OrderBy(post => post.CreatedAt).ThenBy(post => post.Id)
            .Select(post => new DiscussionView(
                post.Id,
                post.AuthorId,
                post.Author.FirstName + " " + post.Author.LastName,
                post.Content,
                post.CreatedAt,
                post.UpdatedAt))
            .ToListAsync(cancellationToken);

        return posts;
    }

    public async Task<DiscussionView> AddPostAsync(Guid positionId, DiscussionRequest request, CancellationToken cancellationToken = default) {
        await EnsureAccessAsync(positionId, cancellationToken);

        if (string.IsNullOrWhiteSpace(request.Content) || request.Content.Length > 10000)
            throw new ValidationException("Post must contain 1–10000 characters.");

        var authorId = currentUser.RequireUserId();
        var post = new DiscussionPost {
            Id = Guid.NewGuid(),
            PositionId = positionId,
            AuthorId = authorId,
            Content = request.Content.Trim()
        };

        db.DiscussionPosts.Add(post);
        await db.SaveChangesAsync(cancellationToken);

        var author = await db.Users.AsNoTracking().FirstAsync(user => user.Id == authorId, cancellationToken);
        return new DiscussionView(post.Id, authorId, author.FirstName + " " + author.LastName, post.Content, post.CreatedAt, null);
    }

    public async Task<DiscussionView> UpdatePostAsync(Guid id, DiscussionRequest request, CancellationToken cancellationToken = default) {
        var post = await db.DiscussionPosts.Include(item => item.Author).FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (post is null)
            throw new NotFoundException("Post not found.");

        if (post.AuthorId != currentUser.RequireUserId() && !currentUser.IsAdmin)
            throw new ForbiddenException();

        if (string.IsNullOrWhiteSpace(request.Content) || request.Content.Length > 10000)
            throw new ValidationException("Post must contain 1–10000 characters.");

        post.Content = request.Content.Trim();
        post.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        return new DiscussionView(post.Id, post.AuthorId, post.Author.FirstName + " " + post.Author.LastName, post.Content, post.CreatedAt, post.UpdatedAt);
    }

    public async Task DeletePostAsync(Guid id, CancellationToken cancellationToken = default) {
        var post = await db.DiscussionPosts.FindAsync([id], cancellationToken);
        if (post is null)
            throw new NotFoundException("Post not found.");

        if (post.AuthorId != currentUser.RequireUserId() && !currentUser.IsAdmin)
            throw new ForbiddenException();

        db.DiscussionPosts.Remove(post);
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureAccessAsync(Guid positionId, CancellationToken cancellationToken) {
        var positions = db.Positions.AsNoTracking().Where(position => position.Id == positionId);
        var userId = currentUser.RequireUserId();

        if (!currentUser.IsRecruiter && !currentUser.IsAdmin)
            positions = PositionAccessHelper.Eligible(positions, db, userId);

        if (!await positions.AnyAsync(cancellationToken))
            throw new NotFoundException("Position not found.");
    }
}
