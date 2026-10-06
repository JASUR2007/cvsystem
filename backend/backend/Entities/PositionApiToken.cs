namespace backend.Entities;

public sealed class PositionApiToken {
    public Guid Id { get; set; }
    public Guid PositionId { get; set; }
    public Position Position { get; set; } = null!;
    public string TokenHash { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastUsedAt { get; set; }
    public bool IsActive { get; set; } = true;
}
