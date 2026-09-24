using backend.Auth;
using backend.Common.Enums;
using backend.Common.Exceptions;
using backend.Common.Pagination;
using backend.Data;
using backend.DTOs.Admin;
using backend.Entities;
using backend.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace backend.Services;

public class AdminUserService(
    AppDbContext db,
    UserManager<AppUser> userManager,
    ICurrentUserService currentUser) : IAdminUserService {
    private static readonly HashSet<string> AllowedRoles =
    [
        Roles.Candidate,
        Roles.Recruiter,
        Roles.Administrator
    ];

    public async Task<AdminDashboardResponse> GetDashboardAsync(CancellationToken cancellationToken = default) {
        var totalUsers = await db.Users.CountAsync(cancellationToken);
        var candidates = await (from link in db.UserRoles
                                join r in db.Roles on link.RoleId equals r.Id
                                where r.Name == Roles.Candidate
                                select link.UserId).Distinct().CountAsync(cancellationToken);
        var recruiters = await (from link in db.UserRoles
                                join r in db.Roles on link.RoleId equals r.Id
                                where r.Name == Roles.Recruiter
                                select link.UserId).Distinct().CountAsync(cancellationToken);
        var admins = await (from link in db.UserRoles
                            join r in db.Roles on link.RoleId equals r.Id
                            where r.Name == Roles.Administrator
                            select link.UserId).Distinct().CountAsync(cancellationToken);
        var blockedUsers = await db.Users.CountAsync(u => u.IsBlocked, cancellationToken);
        var positions = await db.Positions.CountAsync(cancellationToken);
        var draftCvs = await db.Cvs.CountAsync(cv => cv.Status == CvStatus.Draft, cancellationToken);
        var publishedCvs = await db.Cvs.CountAsync(cv => cv.Status == CvStatus.Published, cancellationToken);

        var recentUsers = await db.Users.AsNoTracking()
            .OrderBy(u => u.Email)
            .Take(5)
            .Select(u => new AdminRecentUser(
                u.Id,
                u.FirstName + " " + u.LastName,
                (from link in db.UserRoles
                 join r in db.Roles on link.RoleId equals r.Id
                 where link.UserId == u.Id
                 select r.Name).FirstOrDefault() ?? "User"))
            .ToListAsync(cancellationToken);

        var recentPositions = await db.Positions.AsNoTracking()
            .OrderByDescending(p => p.CreatedAt)
            .Take(5)
            .Select(p => new AdminRecentPosition(
                p.Id,
                p.Title,
                p.Level == null ? null : p.Level.ToString(),
                p.Cvs.Count(cv => cv.Status == CvStatus.Published)))
            .ToListAsync(cancellationToken);

        return new AdminDashboardResponse(
            totalUsers,
            candidates,
            recruiters,
            admins,
            blockedUsers,
            positions,
            draftCvs,
            publishedCvs,
            recentUsers,
            recentPositions);
    }

    public async Task<PagedResult<AdminUserView>> ListUsersAsync(
        string? q, string? role, bool? isBlocked, int page, int pageSize,
        CancellationToken cancellationToken = default) {
        var query = db.Users.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(user => user.SearchVector.Matches(q.Trim()) || (user.Email != null && EF.Functions.ILike(user.Email, "%" + q.Trim() + "%")));

        if (isBlocked is not null)
            query = query.Where(user => user.IsBlocked == isBlocked.Value);

        if (!string.IsNullOrWhiteSpace(role)) {
            var roleId = await db.Roles.Where(r => r.Name == role).Select(r => r.Id).FirstOrDefaultAsync(cancellationToken);
            if (roleId != Guid.Empty)
                query = query.Where(user => db.UserRoles.Any(link => link.UserId == user.Id && link.RoleId == roleId));
        }

        var currentPage = Math.Max(1, page);
        var size = Math.Clamp(pageSize, 1, 100);
        var total = await query.CountAsync(cancellationToken);

        var users = await query.OrderBy(user => user.Email).Skip((currentPage - 1) * size).Take(size)
            .Select(user => new { user.Id, user.Email, user.FirstName, user.LastName, user.IsBlocked })
            .ToListAsync(cancellationToken);

        var ids = users.Select(user => user.Id).ToList();
        var roles = await (from link in db.UserRoles
                           join r in db.Roles on link.RoleId equals r.Id
                           where ids.Contains(link.UserId)
                           select new { link.UserId, r.Name }).ToListAsync(cancellationToken);

        var statuses = await db.UserClaims
            .Where(c => c.ClaimType == "recruiter_request_status" && ids.Contains(c.UserId))
            .ToDictionaryAsync(c => c.UserId, c => c.ClaimValue, cancellationToken);

        var items = users.Select(user => {
            var userRoles = roles.Where(r => r.UserId == user.Id).Select(r => r.Name).ToList();
            var status = userRoles.Contains(Roles.Recruiter)
                ? "Approved"
                : (statuses.TryGetValue(user.Id, out var s) ? s : "None");
            return new AdminUserView(
                user.Id,
                user.Email ?? string.Empty,
                user.FirstName,
                user.LastName,
                user.IsBlocked,
                userRoles,
                status);
        }).ToList();

        return new PagedResult<AdminUserView>(items, currentPage, size, total);
    }

    public async Task<AdminUserView> GetUserAsync(Guid id, CancellationToken cancellationToken = default) {
        var user = await userManager.FindByIdAsync(id.ToString());
        if (user is null)
            throw new NotFoundException("User not found.");

        var roles = (await userManager.GetRolesAsync(user)).ToList();
        var status = roles.Contains(Roles.Recruiter)
            ? "Approved"
            : (await db.UserClaims.Where(c => c.UserId == id && c.ClaimType == "recruiter_request_status").Select(c => c.ClaimValue).FirstOrDefaultAsync(cancellationToken) ?? "None");

        return new AdminUserView(user.Id, user.Email ?? string.Empty, user.FirstName, user.LastName, user.IsBlocked, roles, status);
    }

    public async Task SetBlockedAsync(Guid id, bool blocked, CancellationToken cancellationToken = default) {
        if (id == currentUser.UserId)
            throw new ValidationException("You cannot block your own account.");

        var user = await userManager.FindByIdAsync(id.ToString());
        if (user is null)
            throw new NotFoundException("User not found.");

        user.IsBlocked = blocked;
        await userManager.UpdateSecurityStampAsync(user);
        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
            throw new ConflictException("User state changed in another session.");
    }

    public async Task<AdminUserView> UpdateRolesAsync(Guid id, List<string> roles, CancellationToken cancellationToken = default) {
        if (id == currentUser.UserId && !roles.Contains(Roles.Administrator))
            throw new ValidationException("You cannot remove Administrator from your own account.");

        if (roles.Any(role => !AllowedRoles.Contains(role)))
            throw new ValidationException("One or more roles are invalid.");

        var user = await userManager.FindByIdAsync(id.ToString());
        if (user is null)
            throw new NotFoundException("User not found.");

        var isCurrentlyAdmin = await userManager.IsInRoleAsync(user, Roles.Administrator);
        if (isCurrentlyAdmin && !roles.Contains(Roles.Administrator) && await AdminCountAsync(cancellationToken) <= 1)
            throw new ConflictException("The system must have at least one active administrator.");

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        var existing = await userManager.GetRolesAsync(user);
        var remove = await userManager.RemoveFromRolesAsync(user, existing);
        if (!remove.Succeeded)
            throw new ConflictException("Roles could not be cleared.");

        var add = await userManager.AddToRolesAsync(user, roles);
        if (!add.Succeeded)
            throw new ConflictException("Roles could not be assigned.");

        user.SecurityStamp = Guid.NewGuid().ToString("N");
        var saved = await userManager.UpdateAsync(user);
        if (!saved.Succeeded)
            throw new ConflictException("Roles changed in another session.");

        await transaction.CommitAsync(cancellationToken);

        return await GetUserAsync(id, cancellationToken);
    }

    public async Task<AdminUserView> ApproveRecruiterAsync(Guid id, CancellationToken cancellationToken = default) {
        var user = await userManager.FindByIdAsync(id.ToString());
        if (user is null)
            throw new NotFoundException("User not found.");

        if (!await userManager.IsInRoleAsync(user, Roles.Recruiter)) {
            var add = await userManager.AddToRoleAsync(user, Roles.Recruiter);
            if (!add.Succeeded)
                throw new ConflictException("Failed to assign Recruiter role.");
        }

        var existingClaim = (await userManager.GetClaimsAsync(user))
            .FirstOrDefault(c => c.Type == "recruiter_request_status");
        if (existingClaim is not null) {
            await userManager.ReplaceClaimAsync(user, existingClaim, new System.Security.Claims.Claim("recruiter_request_status", "Approved"));
        }
        else {
            await userManager.AddClaimAsync(user, new System.Security.Claims.Claim("recruiter_request_status", "Approved"));
        }

        user.SecurityStamp = Guid.NewGuid().ToString("N");
        user.AuthVersion++;
        await userManager.UpdateAsync(user);

        return await GetUserAsync(id, cancellationToken);
    }

    public async Task<AdminUserView> RejectRecruiterAsync(Guid id, CancellationToken cancellationToken = default) {
        var user = await userManager.FindByIdAsync(id.ToString());
        if (user is null)
            throw new NotFoundException("User not found.");

        if (await userManager.IsInRoleAsync(user, Roles.Recruiter)) {
            await userManager.RemoveFromRoleAsync(user, Roles.Recruiter);
        }

        var existingClaim = (await userManager.GetClaimsAsync(user))
            .FirstOrDefault(c => c.Type == "recruiter_request_status");
        if (existingClaim is not null) {
            await userManager.ReplaceClaimAsync(user, existingClaim, new System.Security.Claims.Claim("recruiter_request_status", "Rejected"));
        }
        else {
            await userManager.AddClaimAsync(user, new System.Security.Claims.Claim("recruiter_request_status", "Rejected"));
        }

        user.SecurityStamp = Guid.NewGuid().ToString("N");
        user.AuthVersion++;
        await userManager.UpdateAsync(user);

        return await GetUserAsync(id, cancellationToken);
    }

    public async Task DeleteUserAsync(Guid id, CancellationToken cancellationToken = default) {
        if (id == currentUser.UserId)
            throw new ValidationException("You cannot delete your own account.");

        var user = await userManager.FindByIdAsync(id.ToString());
        if (user is null)
            throw new NotFoundException("User not found.");

        if (await userManager.IsInRoleAsync(user, Roles.Administrator) && await AdminCountAsync(cancellationToken) <= 1)
            throw new ConflictException("The last administrator cannot be deleted.");

        if (await db.DiscussionPosts.AnyAsync(post => post.AuthorId == id, cancellationToken)
            || await db.CvLikes.AnyAsync(like => like.RecruiterId == id, cancellationToken))
            throw new ConflictException("User has posts or likes. Block the account instead.");

        var result = await userManager.DeleteAsync(user);
        if (!result.Succeeded)
            throw new ConflictException("User could not be deleted.");
    }

    private Task<int> AdminCountAsync(CancellationToken cancellationToken) =>
        (from link in db.UserRoles
         join role in db.Roles on link.RoleId equals role.Id
         where role.Name == Roles.Administrator
         select link.UserId)
        .Distinct().CountAsync(cancellationToken);
}
