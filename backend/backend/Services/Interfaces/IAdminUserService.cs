using backend.Common.Pagination;
using backend.DTOs.Admin;

namespace backend.Services.Interfaces;

public interface IAdminUserService
{
    Task<PagedResult<AdminUserView>> ListUsersAsync(
        string? q, string? role, bool? isBlocked, int page, int pageSize,
        CancellationToken cancellationToken = default);

    Task<AdminUserView> GetUserAsync(Guid id, CancellationToken cancellationToken = default);
    Task SetBlockedAsync(Guid id, bool blocked, CancellationToken cancellationToken = default);
    Task<AdminUserView> UpdateRolesAsync(Guid id, List<string> roles, CancellationToken cancellationToken = default);
    Task DeleteUserAsync(Guid id, CancellationToken cancellationToken = default);
}
