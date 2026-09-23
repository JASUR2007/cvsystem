namespace backend.Data;

public enum PositionLevel
{
    Junior,
    Middle,
    Senior,
    Lead
}

public enum AccessOperator
{
    Equals,
    NotEquals,
    GreaterThan,
    GreaterThanOrEqual,
    LessThan,
    LessThanOrEqual,
    Contains
}

public sealed class Position
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ShortDescription { get; set; } = string.Empty;
    public string? Company { get; set; }
    public PositionLevel? Level { get; set; }
    public bool IsPublic { get; set; } = true;
    public int MaxProjects { get; set; } = 3;
    public int Version { get; set; } = 1;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public NpgsqlTypes.NpgsqlTsVector SearchVector { get; set; } = null!;
    public ICollection<PositionAttribute> Attributes { get; set; } = [];
    public ICollection<PositionAccessRule> AccessRules { get; set; } = [];
    public ICollection<PositionProjectTag> ProjectTags { get; set; } = [];
    public ICollection<Cv> Cvs { get; set; } = [];
    public ICollection<DiscussionPost> DiscussionPosts { get; set; } = [];
}

public sealed class PositionAttribute
{
    public Guid PositionId { get; set; }
    public Position Position { get; set; } = null!;
    public Guid AttributeId { get; set; }
    public AttributeDefinition Attribute { get; set; } = null!;
    public int SortOrder { get; set; }
    public bool IsRequired { get; set; } = true;
}

public sealed class PositionAccessRule
{
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
