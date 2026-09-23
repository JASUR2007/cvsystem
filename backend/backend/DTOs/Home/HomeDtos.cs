namespace backend.DTOs.Home;

public sealed record HomeStatisticsResponse(
    int Users,
    int Candidates,
    int Recruiters,
    int Positions,
    int PublishedCvs,
    int CvsLast24Hours);

public sealed record PopularPositionResponse(
    Guid Id,
    string Title,
    int CvCount);

public sealed record TagResponse(
    string Name,
    int Count);
