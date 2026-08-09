namespace Wiley.Apartments.Domain;

public class AuditLog
{
    public long Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public DateTime TimestampUtc { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
}
