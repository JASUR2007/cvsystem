namespace backend.Entities;

public sealed class UserAttributeValue {
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
