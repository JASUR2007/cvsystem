namespace backend.Entities;

public sealed class DiscussionPost
{
    public Guid Id { get; set; }
    public Guid PositionId { get; set; }
    public Position Position { get; set; } = null!;
    public Guid AuthorId { get; set; }
    public AppUser Author { get; set; } = null!;
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
