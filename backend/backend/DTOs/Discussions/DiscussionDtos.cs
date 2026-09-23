namespace backend.DTOs.Discussions;

public sealed record DiscussionRequest(string Content);

public sealed record DiscussionView(
    Guid Id,
    Guid AuthorId,
    string Author,
    string Content,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
