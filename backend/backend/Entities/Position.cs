using backend.Common.Enums;

namespace backend.Entities;

public sealed class Position {
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ShortDescription { get; set; } = string.Empty;
    public string? Company { get; set; }
    public PositionLevel? Level { get; set; }
    public bool IsPublic { get; set; } = true;
    public int MaxProjects { get; set; } = 3;
    public int Version { get; set; } = 1;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public NpgsqlTypes.NpgsqlTsVector SearchVector { get; set; } = null!;
    public ICollection<PositionAttribute> Attributes { get; set; } = [];
    public ICollection<PositionAccessRule> AccessRules { get; set; } = [];
    public ICollection<PositionProjectTag> ProjectTags { get; set; } = [];
    public ICollection<Cv> Cvs { get; set; } = [];
    public ICollection<DiscussionPost> DiscussionPosts { get; set; } = [];
}
