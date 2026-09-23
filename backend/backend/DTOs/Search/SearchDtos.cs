namespace backend.DTOs.Search;

public sealed record SearchCvItem(
    Guid Id,
    Guid PositionId,
    string Position,
    string Candidate,
    DateTime UpdatedAt,
    int Likes);
