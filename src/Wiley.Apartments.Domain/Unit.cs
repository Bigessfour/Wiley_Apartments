namespace Wiley.Apartments.Domain;

public class Unit
{
    public Guid Id { get; set; }
    public string Number { get; set; } = string.Empty;
    public decimal SqFt { get; set; }
    public int Beds { get; set; }
    public int Baths { get; set; }
    public UnitStatus Status { get; set; } = UnitStatus.Vacant;
    public string? Notes { get; set; }
    public Guid? CurrentTenantId { get; set; }
    /// <summary>Optimistic concurrency token (SQLite-friendly Guid).</summary>
    public Guid RowVersion { get; set; } = Guid.NewGuid();
}
