using backend.Api;
using backend.Auth;
using backend.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers;

[ApiController]
[Authorize(Roles = Roles.Administrator)]
[Route("api/admin")]
public sealed class AdminController(AppDbContext db, UserManager<AppUser> userManager) : ControllerBase
{
    private static readonly string[] AllowedRoles = [Roles.Candidate, Roles.Recruiter, Roles.Administrator];

    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard(CancellationToken cancellationToken)
    {
        var users = await db.Users.CountAsync(cancellationToken);
        var blocked = await db.Users.CountAsync(user => user.IsBlocked, cancellationToken);
        var positions = await db.Positions.CountAsync(cancellationToken);
        var attributes = await db.Attributes.CountAsync(cancellationToken);
        var published = await db.Cvs.CountAsync(cv => cv.Status == CvStatus.Published, cancellationToken);
        var drafts = await db.Cvs.CountAsync(cv => cv.Status == CvStatus.Draft, cancellationToken);
        return Ok(new { users, blocked, positions, attributes, published, drafts });
    }

    [HttpGet("users")]
    public async Task<IActionResult> Users(string? q, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var query = db.Users.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(q)) query = query.Where(user => EF.Functions.ILike(user.Email!, "%" + q.Trim() + "%")
            || EF.Functions.ILike(user.FirstName + " " + user.LastName, "%" + q.Trim() + "%"));
        var currentPage = ApiModels.Page(page);
        var size = ApiModels.PageSize(pageSize);
        var total = await query.CountAsync(cancellationToken);
        var users = await query.OrderBy(user => user.Email).Skip((currentPage - 1) * size).Take(size)
            .Select(user => new { user.Id, user.Email, user.FirstName, user.LastName, user.IsBlocked }).ToListAsync(cancellationToken);
        var ids = users.Select(user => user.Id).ToList();
        var roles = await (from link in db.UserRoles
                           join role in db.Roles on link.RoleId equals role.Id
                           where ids.Contains(link.UserId)
                           select new { link.UserId, role.Name }).ToListAsync(cancellationToken);
        var items = users.Select(user => new AdminUserView(user.Id, user.Email ?? string.Empty, user.FirstName, user.LastName,
            user.IsBlocked, roles.Where(role => role.UserId == user.Id).Select(role => role.Name!).ToList())).ToList();
        return Ok(new PageResult<AdminUserView>(items, currentPage, size, total));
    }

    [HttpGet("users/{id:guid}")]
    public async Task<IActionResult> GetUser(Guid id, CancellationToken cancellationToken)
    {
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (user is null) return NotFound();
        var roles = await userManager.GetRolesAsync(user);
        return Ok(new AdminUserView(user.Id, user.Email ?? string.Empty, user.FirstName, user.LastName, user.IsBlocked, roles.ToList()));
    }

    [HttpPost("users/{id:guid}/block")]
    public Task<IActionResult> Block(Guid id, CancellationToken cancellationToken) => SetBlocked(id, true, cancellationToken);

    [HttpPost("users/{id:guid}/unblock")]
    public Task<IActionResult> Unblock(Guid id, CancellationToken cancellationToken) => SetBlocked(id, false, cancellationToken);

    [HttpPut("users/{id:guid}/roles")]
    public async Task<IActionResult> RolesUpdate(Guid id, RoleUpdate request, CancellationToken cancellationToken)
    {
        if (request.Roles.Count == 0 || request.Roles.Except(AllowedRoles).Any()) return BadRequest(new { message = "Invalid roles." });
        var user = await userManager.FindByIdAsync(id.ToString());
        if (user is null) return NotFound();
        var currentRoles = await userManager.GetRolesAsync(user);
        if (id == ApiModels.UserId(User) && !request.Roles.Contains(Roles.Administrator))
            return BadRequest(new { message = "You cannot remove your own administrator role." });
        if (currentRoles.Contains(Roles.Administrator) && !request.Roles.Contains(Roles.Administrator)
            && await AdminCount(cancellationToken) <= 1) return Conflict(new { message = "The last administrator cannot be removed." });

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var removed = await userManager.RemoveFromRolesAsync(user, currentRoles.Except(request.Roles));
        if (!removed.Succeeded) return BadRequest(new { errors = removed.Errors.Select(error => error.Description) });
        var added = await userManager.AddToRolesAsync(user, request.Roles.Except(currentRoles));
        if (!added.Succeeded) return BadRequest(new { errors = added.Errors.Select(error => error.Description) });
        user.AuthVersion++;
        var saved = await userManager.UpdateAsync(user);
        if (!saved.Succeeded) return Conflict(new { message = "Roles changed in another session." });
        await transaction.CommitAsync(cancellationToken);
        return Ok(new AdminUserView(user.Id, user.Email ?? string.Empty, user.FirstName, user.LastName, user.IsBlocked, request.Roles));
    }

    [HttpDelete("users/{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        if (id == ApiModels.UserId(User)) return BadRequest(new { message = "You cannot delete your own account." });
        var user = await userManager.FindByIdAsync(id.ToString());
        if (user is null) return NotFound();
        if (await userManager.IsInRoleAsync(user, Roles.Administrator) && await AdminCount(cancellationToken) <= 1)
            return Conflict(new { message = "The last administrator cannot be deleted." });
        if (await db.DiscussionPosts.AnyAsync(post => post.AuthorId == id, cancellationToken)
            || await db.CvLikes.AnyAsync(like => like.RecruiterId == id, cancellationToken))
            return Conflict(new { message = "User has posts or likes. Block the account instead." });
        var result = await userManager.DeleteAsync(user);
        return result.Succeeded ? NoContent() : Conflict(new { message = "User could not be deleted." });
    }

    private async Task<IActionResult> SetBlocked(Guid id, bool blocked, CancellationToken cancellationToken)
    {
        if (id == ApiModels.UserId(User) && blocked) return BadRequest(new { message = "You cannot block your own account." });
        var user = await db.Users.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (user is null) return NotFound();
        user.IsBlocked = blocked;
        user.AuthVersion++;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { user.Id, user.IsBlocked });
    }

    private Task<int> AdminCount(CancellationToken cancellationToken) =>
        (from link in db.UserRoles join role in db.Roles on link.RoleId equals role.Id where role.Name == Roles.Administrator select link.UserId)
        .Distinct().CountAsync(cancellationToken);
}

public sealed record AdminUserView(Guid Id, string Email, string FirstName, string LastName, bool IsBlocked, List<string> Roles);
public sealed record RoleUpdate(List<string> Roles);
