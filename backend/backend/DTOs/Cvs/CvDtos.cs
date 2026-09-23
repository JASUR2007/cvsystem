using backend.Common.Enums;
using backend.DTOs.Attributes;
using backend.DTOs.Positions;
using backend.DTOs.Profile;
using backend.DTOs.Projects;

namespace backend.DTOs.Cvs;

public sealed record CvListItem(
    Guid Id,
    Guid PositionId,
    string Position,
    CvStatus Status,
    DateTime UpdatedAt,
    int Likes);

public sealed record CvAttributeView(
    AttributeValueView Value,
    bool IsRequired,
    bool IsFilled,
    List<AttributeOptionView> Options);

public sealed record CvDetail(
    Guid Id,
    Guid PositionId,
    string Position,
    Guid CandidateId,
    string FirstName,
    string LastName,
    string? Location,
    string? PhotoObjectKey,
    CvStatus Status,
    DateTime UpdatedAt,
    int Likes,
    bool CanEdit,
    List<CvAttributeView> Attributes,
    List<ProjectView> Projects);

public sealed record PositionCvCell(Guid AttributeId, string? Value);

public sealed record PositionCvRow(
    Guid Id,
    Guid CandidateId,
    string Candidate,
    DateTime UpdatedAt,
    int Likes,
    List<PositionCvCell> Values);

public sealed record PositionCvPage(
    List<PositionAttributeView> Columns,
    List<PositionCvRow> Items,
    int Page,
    int PageSize,
    int TotalItems);
