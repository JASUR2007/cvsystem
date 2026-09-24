namespace backend.Entities;

public sealed class CvLike {
    public Guid CvId { get; set; }
    public Cv Cv { get; set; } = null!;
    public Guid RecruiterId { get; set; }
    public AppUser Recruiter { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
