namespace Wiley.Apartments.Domain;

/// <summary>Landlord ops expense (T4.5). Never mixed into tenant <see cref="LedgerEntry"/> balances.</summary>
public class UnitOperatingCost
{
    public Guid Id { get; set; }
    /// <summary>Null only when <see cref="Category"/> is <see cref="OperatingCostCategory.CommonUpkeep"/> (building-wide).</summary>
    public Guid? UnitId { get; set; }
    public Unit? Unit { get; set; }
    public OperatingCostCategory Category { get; set; }
    public decimal Amount { get; set; }
    public DateTime IncurredUtc { get; set; }
    public string? Vendor { get; set; }
    public string? Notes { get; set; }
    public Guid? MaintenanceRequestId { get; set; }
    public bool IsDeleted { get; set; }
}
