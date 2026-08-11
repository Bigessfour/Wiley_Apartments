namespace Wiley.Apartments.Contracts;

/// <summary>Clerk home dashboard snapshot (T6.2).</summary>
public interface IDashboardService
{
    Task<DashboardSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default);
}

public sealed class DashboardSnapshot
{
    public int TotalUnits { get; init; }
    public int OccupiedCount { get; init; }
    public int VacantCount { get; init; }
    public int MakeReadyCount { get; init; }
    public int MaintenanceCount { get; init; }
    public double OccupancyRate { get; init; }

    /// <summary>Open work orders — 0 until maintenance module exists.</summary>
    public int OpenWorkOrders { get; init; }

    /// <summary>Delinquent unit count — 0 until payments module exists.</summary>
    public int DelinquentCount { get; init; }

    /// <summary>Delinquent balance total — 0 until payments module exists.</summary>
    public decimal DelinquentAmount { get; init; }

    public IReadOnlyList<WarrantyAlertItem> WarrantyAlerts { get; init; } = Array.Empty<WarrantyAlertItem>();
    public IReadOnlyList<UnitStatusRow> Units { get; init; } = Array.Empty<UnitStatusRow>();
    public IReadOnlyList<StatusBreakdownItem> StatusBreakdown { get; init; } = Array.Empty<StatusBreakdownItem>();
}

public sealed class WarrantyAlertItem
{
    public Guid AssetId { get; init; }
    public Guid UnitId { get; init; }
    public string UnitNumber { get; init; } = string.Empty;
    public string AssetType { get; init; } = string.Empty;
    public DateOnly WarrantyEnd { get; init; }
    public int DaysLeft { get; init; }
}

public sealed class UnitStatusRow
{
    public Guid Id { get; init; }
    public string Number { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public int Beds { get; init; }
    public int Baths { get; init; }
    public decimal SqFt { get; init; }
}

public sealed class StatusBreakdownItem
{
    public string Name { get; init; } = string.Empty;
    public int Count { get; init; }
}
