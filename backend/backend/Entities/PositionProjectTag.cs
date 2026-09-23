namespace backend.Entities;

public sealed class PositionProjectTag
{
    public Guid PositionId { get; set; }
    public Position Position { get; set; } = null!;
    public Guid TagId { get; set; }
    public Tag Tag { get; set; } = null!;
}
