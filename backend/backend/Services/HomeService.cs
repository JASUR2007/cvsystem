using backend.Auth;
using backend.Data;
using backend.DTOs.Home;
using backend.DTOs.Positions;
using backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace backend.Services;

public class HomeService(AppDbContext db) : IHomeService {
    public async Task<HomeStatisticsResponse> GetStatisticsAsync(CancellationToken cancellationToken = default) {
        var users = await db.Users.CountAsync(cancellationToken);
        var candidates = await db.UserRoles.Join(db.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => r).CountAsync(r => r.Name == Roles.Candidate, cancellationToken);
        var recruiters = await db.UserRoles.Join(db.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => r).CountAsync(r => r.Name == Roles.Recruiter, cancellationToken);
        var positions = await db.Positions.CountAsync(cancellationToken);
        var publishedCvs = await db.Cvs.CountAsync(cancellationToken);
        var cvsLast24Hours = await db.Cvs.CountAsync(c => c.CreatedAt >= DateTime.UtcNow.AddDays(-1), cancellationToken);

        return new HomeStatisticsResponse(users, candidates, recruiters, positions, publishedCvs, cvsLast24Hours);
    }

    public async Task<List<PositionListItem>> GetLatestPositionsAsync(CancellationToken cancellationToken = default) {
        var positions = await db.Positions.AsNoTracking()
            .OrderByDescending(p => p.UpdatedAt)
            .Take(10)
            .Select(p => new PositionListItem(
                p.Id,
                p.Title,
                p.Company,
                p.Level,
                p.UpdatedAt,
                p.IsPublic,
                p.Cvs.Count))
            .ToListAsync(cancellationToken);

        return positions;
    }

    public async Task<List<PopularPositionResponse>> GetPopularPositionsAsync(CancellationToken cancellationToken = default) {
        var positions = await db.Positions.AsNoTracking()
            .OrderByDescending(p => p.Cvs.Count)
            .Take(5)
            .Select(p => new PopularPositionResponse(p.Id, p.Title, p.Cvs.Count))
            .ToListAsync(cancellationToken);

        return positions;
    }

    public async Task<List<TagResponse>> GetPopularTagsAsync(CancellationToken cancellationToken = default) {
        var tags = await db.Tags.AsNoTracking()
            .OrderByDescending(t => t.Projects.Count)
            .Take(20)
            .Select(t => new TagResponse(t.Name, t.Projects.Count))
            .ToListAsync(cancellationToken);

        return tags;
    }
}
