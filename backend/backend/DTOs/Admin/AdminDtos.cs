namespace backend.DTOs.Admin;

public sealed record AdminUserView(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    bool IsBlocked,
    List<string> Roles);

public sealed record RoleUpdate(List<string> Roles);
