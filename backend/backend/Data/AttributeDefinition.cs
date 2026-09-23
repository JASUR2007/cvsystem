namespace backend.Data;

public enum AttributeType
{
    String,
    Text,
    Image,
    Numeric,
    Date,
    Period,
    Boolean,
    Dropdown
}

public sealed class AttributeDefinition
{
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

public sealed class AttributeOption
{
    public Guid Id { get; set; }
    public Guid AttributeId { get; set; }
    public AttributeDefinition Attribute { get; set; } = null!;
    public string Value { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}

public sealed class UserAttributeValue
{
    public Guid UserId { get; set; }
    public AppUser User { get; set; } = null!;
    public Guid AttributeId { get; set; }
    public AttributeDefinition Attribute { get; set; } = null!;
    public string? TextValue { get; set; }
    public decimal? NumberValue { get; set; }
    public DateOnly? DateValue { get; set; }
    public DateOnly? PeriodStart { get; set; }
    public DateOnly? PeriodEnd { get; set; }
    public bool? BooleanValue { get; set; }
    public Guid? SelectedOptionId { get; set; }
    public AttributeOption? SelectedOption { get; set; }
    public string? ImageObjectKey { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public int Version { get; set; } = 1;
}
