namespace backend.Entities;

public sealed class PositionAttribute {
    public Guid PositionId { get; set; }
    public Position Position { get; set; } = null!;
    public Guid AttributeId { get; set; }
    public AttributeDefinition Attribute { get; set; } = null!;
    public int SortOrder { get; set; }
    public bool IsRequired { get; set; } = true;
}
