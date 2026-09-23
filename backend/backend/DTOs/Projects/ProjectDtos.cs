namespace backend.DTOs.Projects;

public sealed record ProjectRequest(
    string Name,
    DateOnly StartedOn,
    DateOnly? EndedOn,
    string? Description,
    List<string> Tags,
    int Version);

public sealed record ProjectView(
    Guid Id,
    string Name,
    DateOnly StartedOn,
    DateOnly? EndedOn,
    string Description,
    List<string> Tags,
    int Version);
