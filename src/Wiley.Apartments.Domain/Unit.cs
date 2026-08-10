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

    /// <summary>Listed monthly rent from clerk roster (used when no active lease exists).</summary>
    public decimal MonthlyRent { get; set; }

    /// <summary>Security deposit on file for the unit / current occupant.</summary>
    public decimal SecurityDeposit { get; set; }

    /// <summary>Handicap-accessible unit (Brookside ledger flag).</summary>
    public bool IsHandicapAccessible { get; set; }

    /// <summary>Sheet lease term label, e.g. Year or Month-to-Month (paper lease may still exist).</summary>
    public string LeaseTerm { get; set; } = string.Empty;

    /// <summary>
    /// Facility (non-residential) unit such as Community Center. Does not count toward MaxUnits.
    /// </summary>
    public bool IsFacility { get; set; }

    /// <summary>Optimistic concurrency token (SQLite-friendly Guid).</summary>
    public Guid RowVersion { get; set; } = Guid.NewGuid();
}
