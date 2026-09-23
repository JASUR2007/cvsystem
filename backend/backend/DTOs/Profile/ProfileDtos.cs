using backend.Common.Enums;

namespace backend.DTOs.Profile;

public sealed record ProfileUpdate(
    string FirstName,
    string LastName,
    string? Location,
    string? PhotoObjectKey,
    int Version);

public sealed record ProfileView(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string? Location,
    string? PhotoObjectKey,
    string Language,
    string Theme,
    int Version);

public sealed record AttributeValueInput(
    string? TextValue,
    decimal? NumberValue,
    DateOnly? DateValue,
    DateOnly? PeriodStart,
    DateOnly? PeriodEnd,
    bool? BooleanValue,
    Guid? SelectedOptionId,
    string? ImageObjectKey);

public sealed record AttributeValueUpdate(int Version, AttributeValueInput Value);

public sealed record AttributeValueView(
    Guid AttributeId,
    string Name,
    string Category,
    AttributeType Type,
    string? TextValue,
    decimal? NumberValue,
    DateOnly? DateValue,
    DateOnly? PeriodStart,
    DateOnly? PeriodEnd,
    bool? BooleanValue,
    Guid? SelectedOptionId,
    string? ImageObjectKey,
    int Version);
