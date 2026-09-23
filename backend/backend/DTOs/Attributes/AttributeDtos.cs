using backend.Common.Enums;

namespace backend.DTOs.Attributes;

public sealed record AttributeRequest(
    string Name,
    string? Description,
    string Category,
    AttributeType Type,
    List<string> Options,
    int Version);

public sealed record AttributeListItem(
    Guid Id,
    string Name,
    string Category,
    AttributeType Type,
    bool IsBuiltIn,
    int Version,
    int UsageCount);

public sealed record AttributeOptionView(Guid Id, string Value);

public sealed record AttributeDetail(
    Guid Id,
    string Name,
    string Description,
    string Category,
    AttributeType Type,
    bool IsBuiltIn,
    int Version,
    List<AttributeOptionView> Options);
