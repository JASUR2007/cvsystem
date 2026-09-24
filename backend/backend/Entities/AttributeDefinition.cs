using backend.Common.Enums;

namespace backend.Entities;

public sealed class AttributeDefinition {
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public AttributeType Type { get; set; }
    public bool IsBuiltIn { get; set; }
    public int Version { get; set; } = 1;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<AttributeOption> Options { get; set; } = [];
    public ICollection<UserAttributeValue> UserValues { get; set; } = [];
    public ICollection<PositionAttribute> PositionAttributes { get; set; } = [];
}
