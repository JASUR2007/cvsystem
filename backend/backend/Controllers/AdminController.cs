using backend.Auth;
using backend.Common.Pagination;
using backend.DTOs.Admin;
using backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

[ApiController]
[Authorize(Roles = Roles.Administrator)]
[Route("api/admin")]
public sealed class AdminController(IAdminUserService adminUserService) : ControllerBase
{
    [HttpGet("dashboard")]
    public async Task<ActionResult<AdminDashboardResponse>> Dashboard(CancellationToken cancellationToken)
    {
        var result = await adminUserService.GetDashboardAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("users")]
    public async Task<ActionResult<PagedResult<AdminUserView>>> Users(
        string? q, string? role, bool? isBlocked, int page = 1, int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await adminUserService.ListUsersAsync(q, role, isBlocked, page, pageSize, cancellationToken);
        return Ok(result);
    }

    [HttpGet("users/{id:guid}")]
    public async Task<ActionResult<AdminUserView>> GetUser(Guid id, CancellationToken cancellationToken)
    {
        var result = await adminUserService.GetUserAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpPost("users/{id:guid}/block")]
    public async Task<IActionResult> Block(Guid id, CancellationToken cancellationToken)
    {
        await adminUserService.SetBlockedAsync(id, true, cancellationToken);
        return Ok(new { id, isBlocked = true });
    }

    [HttpPost("users/{id:guid}/unblock")]
    public async Task<IActionResult> Unblock(Guid id, CancellationToken cancellationToken)
    {
        await adminUserService.SetBlockedAsync(id, false, cancellationToken);
        return Ok(new { id, isBlocked = false });
    }

    [HttpPut("users/{id:guid}/roles")]
    public async Task<ActionResult<AdminUserView>> RolesUpdate(Guid id, RoleUpdate request, CancellationToken cancellationToken)
    {
        var result = await adminUserService.UpdateRolesAsync(id, request.Roles, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("users/{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await adminUserService.DeleteUserAsync(id, cancellationToken);
        return NoContent();
    }
}
