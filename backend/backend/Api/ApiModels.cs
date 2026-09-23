using System.Security.Claims;
using backend.Auth;

namespace backend.Api;

public sealed record PageResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalItems)
{
    public int TotalPages => (int)Math.Ceiling((double)TotalItems / PageSize);
}

public static class ApiModels
{
    public static bool ValidImageKey(string? key, Guid userId) =>
        string.IsNullOrWhiteSpace(key) || key.StartsWith($"users/{userId}/", StringComparison.Ordinal);
    public static Guid? UserId(ClaimsPrincipal principal) =>
        Guid.TryParse(principal.FindFirst("sub")?.Value, out var id) ? id : null;

    public static bool IsAdmin(ClaimsPrincipal principal) => principal.IsInRole(Roles.Administrator);

    public static int Page(int value) => Math.Max(1, value);

    public static int PageSize(int value) => Math.Clamp(value, 1, 100);
}
