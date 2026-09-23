using Microsoft.AspNetCore.Identity;

namespace backend.Data;

public sealed class AppUser : IdentityUser<Guid>
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Location { get; set; }
    public string? PhotoObjectKey { get; set; }
    public string Language { get; set; } = "en";
    public string Theme { get; set; } = "light";
    public bool IsBlocked { get; set; }
    public int AuthVersion { get; set; } = 1;
    public int Version { get; set; } = 1;
    public NpgsqlTypes.NpgsqlTsVector SearchVector { get; set; } = null!;
    public ICollection<UserAttributeValue> AttributeValues { get; set; } = [];
    public ICollection<Project> Projects { get; set; } = [];
    public ICollection<Cv> Cvs { get; set; } = [];
}
