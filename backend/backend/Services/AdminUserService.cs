using backend.Auth;
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
    ICurrentUserService currentUser) : IAdminUserService
{
    private static readonly HashSet<string> AllowedRoles =
    [
        Roles.Candidate,
        Roles.Recruiter,
        Roles.Administrator
    ];

    public async Task<PagedResult<AdminUserView>> ListUsersAsync(
        string? q, string? role, bool? isBlocked, int page, int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = db.Users.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(user => user.SearchVector.Matches(q.Trim()) || (user.Email != null && EF.Functions.ILike(user.Email, "%" + q.Trim() + "%")));

        if (isBlocked is not null)
            query = query.Where(user => user.IsBlocked == isBlocked.Value);

        if (!string.IsNullOrWhiteSpace(role))
        {
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

        var items = users.Select(user => new AdminUserView(
            user.Id,
            user.Email ?? string.Empty,
            user.FirstName,
            user.LastName,
            user.IsBlocked,
            roles.Where(r => r.UserId == user.Id).Select(r => r.Name!).ToList())).ToList();

        return new PagedResult<AdminUserView>(items, currentPage, size, total);
    }

    public async Task<AdminUserView> GetUserAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (user is null)
            throw new NotFoundException("User not found.");

        var roles = await userManager.GetRolesAsync(user);
        return new AdminUserView(user.Id, user.Email ?? string.Empty, user.FirstName, user.LastName, user.IsBlocked, roles.ToList());
    }

    public async Task SetBlockedAsync(Guid id, bool blocked, CancellationToken cancellationToken = default)
    {
        if (id == currentUser.UserId && blocked)
            throw new ValidationException("You cannot block your own account.");

        var user = await db.Users.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (user is null)
            throw new NotFoundException("User not found.");

        user.IsBlocked = blocked;
        user.AuthVersion++;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<AdminUserView> UpdateRolesAsync(Guid id, List<string> roles, CancellationToken cancellationToken = default)
    {
        if (roles.Count == 0 || roles.Except(AllowedRoles).Any())
            throw new ValidationException("Invalid roles specified.");

        var user = await userManager.FindByIdAsync(id.ToString());
        if (user is null)
            throw new NotFoundException("User not found.");

        var currentRoles = await userManager.GetRolesAsync(user);
        if (id == currentUser.UserId && !roles.Contains(Roles.Administrator))
            throw new ValidationException("You cannot remove your own administrator role.");

        if (currentRoles.Contains(Roles.Administrator) && !roles.Contains(Roles.Administrator)
            && await AdminCountAsync(cancellationToken) <= 1)
            throw new ConflictException("The last administrator cannot be removed.");

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var removed = await userManager.RemoveFromRolesAsync(user, currentRoles.Except(roles));
        if (!removed.Succeeded)
            throw new ValidationException(string.Join(" ", removed.Errors.Select(e => e.Description)));

        var added = await userManager.AddToRolesAsync(user, roles.Except(currentRoles));
        if (!added.Succeeded)
            throw new ValidationException(string.Join(" ", added.Errors.Select(e => e.Description)));

        user.AuthVersion++;
        var saved = await userManager.UpdateAsync(user);
        if (!saved.Succeeded)
            throw new ConflictException("Roles changed in another session.");

        await transaction.CommitAsync(cancellationToken);

        return new AdminUserView(user.Id, user.Email ?? string.Empty, user.FirstName, user.LastName, user.IsBlocked, roles);
    }

    public async Task DeleteUserAsync(Guid id, CancellationToken cancellationToken = default)
    {
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
