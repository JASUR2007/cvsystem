using backend.Auth;
using backend.Common.Enums;
using backend.Common.Pagination;
using backend.Data;
using backend.DTOs.Positions;
using backend.DTOs.Search;
using backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace backend.Services;

public class SearchService(AppDbContext db, ICurrentUserService currentUser) : ISearchService {
    public async Task<object> SearchAllAsync(string q, CancellationToken cancellationToken = default) {
        var positions = await SearchPositionsAsync(q, 1, 10, cancellationToken);
        if (!currentUser.IsRecruiter && !currentUser.IsAdmin) {
            return new { positions, cvs = new PagedResult<SearchCvItem>([], 1, 10, 0) };
        }

        var cvs = await SearchCvsAsync(q, 1, 10, cancellationToken);
        return new { positions, cvs };
    }

    public async Task<PagedResult<PositionListItem>> SearchPositionsAsync(string q, int page, int pageSize, CancellationToken cancellationToken = default) {
        var query = db.Positions.AsNoTracking().AsQueryable();

        if (!currentUser.IsAuthenticated)
            query = query.Where(position => position.IsPublic);
        else if (currentUser.IsCandidate && !currentUser.IsAdmin)
            query = PositionAccessHelper.Eligible(query, db, currentUser.RequireUserId());

        if (!string.IsNullOrWhiteSpace(q)) {
            var term = q.Trim();
            var pattern = $"%{term}%";
            query = query.Where(position =>
                position.SearchVector.Matches(term)
                || EF.Functions.ILike(position.Title, pattern)
                || EF.Functions.ILike(position.ShortDescription, pattern)
                || (position.Company != null && EF.Functions.ILike(position.Company, pattern))
                || (position.Level != null && EF.Functions.ILike(position.Level.ToString()!, pattern))
            );
        }

        var currentPage = Math.Max(1, page);
        var size = Math.Clamp(pageSize, 1, 100);
        var total = await query.CountAsync(cancellationToken);

        var items = await query.OrderByDescending(position => position.UpdatedAt).Skip((currentPage - 1) * size).Take(size)
            .Select(position => new PositionListItem(
                position.Id,
                position.Title,
                position.Company,
                position.Level,
                position.UpdatedAt,
                position.IsPublic,
                position.Cvs.Count(cv => cv.Status == CvStatus.Published)))
            .ToListAsync(cancellationToken);

        return new PagedResult<PositionListItem>(items, currentPage, size, total);
    }

    public async Task<PagedResult<SearchCvItem>> SearchCvsAsync(string q, int page, int pageSize, CancellationToken cancellationToken = default) {
        var query = db.Cvs.AsNoTracking().Where(cv => cv.Status == CvStatus.Published);
        if (!currentUser.IsAdmin)
            query = PositionAccessHelper.EligibleCvs(query, db);

        if (!string.IsNullOrWhiteSpace(q)) {
            var term = q.Trim();
            var pattern = $"%{term}%";
            query = query.Where(cv =>
                cv.Position.SearchVector.Matches(term)
                || cv.Candidate.SearchVector.Matches(term)
                || EF.Functions.ILike(cv.Position.Title, pattern)
                || EF.Functions.ILike(cv.Candidate.FirstName, pattern)
                || EF.Functions.ILike(cv.Candidate.LastName, pattern)
                || (cv.Candidate.Location != null && EF.Functions.ILike(cv.Candidate.Location, pattern))
                || (cv.Position.Level != null && EF.Functions.ILike(cv.Position.Level.ToString()!, pattern))
            );
        }

        var currentPage = Math.Max(1, page);
        var size = Math.Clamp(pageSize, 1, 100);
        var total = await query.CountAsync(cancellationToken);

        var items = await query.OrderByDescending(cv => cv.UpdatedAt).Skip((currentPage - 1) * size).Take(size)
            .Select(cv => new SearchCvItem(
                cv.Id,
                cv.PositionId,
                cv.Position.Title,
                cv.Candidate.FirstName + " " + cv.Candidate.LastName,
                cv.UpdatedAt,
                cv.Likes.Count))
            .ToListAsync(cancellationToken);

        return new PagedResult<SearchCvItem>(items, currentPage, size, total);
    }
}
