namespace backend.DTOs.Integrations;

public sealed record PositionTokenResponse(
    Guid PositionId,
    string Token,
    DateTime CreatedAt
);

public sealed record PositionTokenStatusResponse(
    bool HasToken,
    DateTime? CreatedAt,
    DateTime? LastUsedAt
);

public sealed record PositionAggregatedResultView(
    Guid PositionId,
    string Title,
    int CvCount,
    DateTime GeneratedAt,
    List<PositionAttributeAggregateView> Attributes
);

public sealed record PositionAttributeAggregateView(
    Guid AttributeId,
    string Title,
    string Type,
    int NonEmptyCount,
    object? Aggregation
);
