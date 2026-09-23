namespace backend.Data;

public sealed class Project
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public AppUser User { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public DateOnly StartedOn { get; set; }
    public DateOnly? EndedOn { get; set; }
    public string Description { get; set; } = string.Empty;
    public int Version { get; set; } = 1;
    public ICollection<ProjectTag> Tags { get; set; } = [];
}

public sealed class Tag
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public ICollection<ProjectTag> Projects { get; set; } = [];
    public ICollection<PositionProjectTag> Positions { get; set; } = [];
}

public sealed class ProjectTag
{
    public Guid ProjectId { get; set; }
    public Project Project { get; set; } = null!;
    public Guid TagId { get; set; }
    public Tag Tag { get; set; } = null!;
}

public sealed class PositionProjectTag
{
    public Guid PositionId { get; set; }
    public Position Position { get; set; } = null!;
    public Guid TagId { get; set; }
    public Tag Tag { get; set; } = null!;
}
