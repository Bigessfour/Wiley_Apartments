namespace Wiley.Apartments.Domain;

public class Occupancy
{
    public Guid Id { get; set; }
    public Guid UnitId { get; set; }
    public Unit? Unit { get; set; }
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }
    public DateTime StartUtc { get; set; }
    public DateTime? EndUtc { get; set; }
}
