namespace backend.Entities;

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
