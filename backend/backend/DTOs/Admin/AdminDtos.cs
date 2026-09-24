namespace backend.DTOs.Admin;

public sealed record AdminUserView(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    bool IsBlocked,
    List<string> Roles,
    string? RecruiterRequestStatus = "None");

public sealed record RoleUpdate(List<string> Roles);

public sealed record AdminRecentUser(Guid Id, string Name, string Role);
public sealed record AdminRecentPosition(Guid Id, string Title, string? Level, int CvCount);

public sealed record AdminDashboardResponse(
    int TotalUsers,
    int Candidates,
    int Recruiters,
    int Administrators,
    int BlockedUsers,
    int Positions,
    int DraftCvs,
    int PublishedCvs,
    List<AdminRecentUser> RecentUsers,
    List<AdminRecentPosition> RecentPositions);
