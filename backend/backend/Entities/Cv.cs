using backend.Common.Enums;

namespace backend.Entities;

public sealed class Cv
{
    public Guid Id { get; set; }
    public Guid CandidateId { get; set; }
    public AppUser Candidate { get; set; } = null!;
    public Guid PositionId { get; set; }
    public Position Position { get; set; } = null!;
    public CvStatus Status { get; set; } = CvStatus.Draft;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? PublishedAt { get; set; }
    public ICollection<CvLike> Likes { get; set; } = [];
}
