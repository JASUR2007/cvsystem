namespace backend.Data;

public enum CvStatus
{
    Draft,
    Published
}

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

public sealed class CvLike
{
    public Guid CvId { get; set; }
    public Cv Cv { get; set; } = null!;
    public Guid RecruiterId { get; set; }
    public AppUser Recruiter { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

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
