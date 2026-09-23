namespace backend.Entities;

public sealed class AttributeOption
{
    public Guid Id { get; set; }
    public Guid AttributeId { get; set; }
    public AttributeDefinition Attribute { get; set; } = null!;
    public string Value { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}
