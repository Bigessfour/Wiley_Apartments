namespace Wiley.Apartments.Domain;

/// <summary>Tenant ledger line (rent charge or payment). Not used for landlord ops costs.</summary>
public class LedgerEntry
{
    public Guid Id { get; set; }
    public LedgerEntryType EntryType { get; set; }
    public Guid? LeaseId { get; set; }
    public Lease? Lease { get; set; }
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }
    public Guid UnitId { get; set; }
    public Unit? Unit { get; set; }
    /// <summary>Always stored as a positive amount; sign applied by <see cref="EntryType"/>.</summary>
    public decimal Amount { get; set; }
    public DateTime DateUtc { get; set; }
    public PaymentMethod? Method { get; set; }
    public string? Notes { get; set; }
    public bool IsLateFee { get; set; }
    public bool IsDeleted { get; set; }
}
