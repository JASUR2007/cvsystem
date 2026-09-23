using backend.Common.Enums;

namespace backend.DTOs.Positions;

public sealed record PositionAttributeInput(
    Guid AttributeId,
    int SortOrder,
    bool IsRequired);

public sealed record PositionAccessRuleInput(
    Guid AttributeId,
    AccessOperator Operator,
    string ComparisonValue);

public sealed record PositionRequest(
    string Title,
    string ShortDescription,
    string? Company,
    PositionLevel? Level,
    bool IsPublic,
    int MaxProjects,
    int Version,
    List<PositionAttributeInput> Attributes,
    List<PositionAccessRuleInput> AccessRules,
    List<string> ProjectTags);

public sealed record PositionListItem(
    Guid Id,
    string Title,
    string? Company,
    PositionLevel? Level,
    DateTime UpdatedAt,
    bool IsPublic,
    int CvCount);

public sealed record PositionAttributeView(
    Guid AttributeId,
    string Name,
    AttributeType Type,
    int SortOrder,
    bool IsRequired);

public sealed record AccessRuleView(
    Guid Id,
    Guid AttributeId,
    string AttributeName,
    AccessOperator Operator,
    string ComparisonValue);

public sealed record PositionDetail(
    Guid Id,
    string Title,
    string ShortDescription,
    string? Company,
    PositionLevel? Level,
    bool IsPublic,
    int MaxProjects,
    int Version,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    List<PositionAttributeView> Attributes,
    List<AccessRuleView> AccessRules,
    List<string> ProjectTags);
