using backend.Common.Enums;

namespace backend.Entities;

public sealed class PositionAccessRule {
    public Guid Id { get; set; }
    public Guid PositionId { get; set; }
    public Position Position { get; set; } = null!;
    public Guid AttributeId { get; set; }
    public AttributeDefinition Attribute { get; set; } = null!;
    public AccessOperator Operator { get; set; }
    public string ComparisonValue { get; set; } = string.Empty;
    public decimal? NumberValue { get; set; }
    public bool? BooleanValue { get; set; }
    public Guid? OptionId { get; set; }
}
