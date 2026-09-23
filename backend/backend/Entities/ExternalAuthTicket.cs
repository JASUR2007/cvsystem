namespace backend.Entities;

public sealed class ExternalAuthTicket
{
    public Guid Id { get; set; }
    public string CodeHash { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public AppUser User { get; set; } = null!;
    public DateTime ExpiresAt { get; set; }
}
