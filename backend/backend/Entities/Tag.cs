namespace backend.Entities;

public sealed class Tag {
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public ICollection<ProjectTag> Projects { get; set; } = [];
    public ICollection<PositionProjectTag> Positions { get; set; } = [];
}
