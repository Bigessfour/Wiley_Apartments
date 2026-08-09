namespace Wiley.Apartments.Contracts;

public interface IDashboardService
{
    Task<DashboardSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default);
}

public sealed record DashboardSnapshot(
    int TotalUnits,
    int Occupied,
    int Vacant,
    int Maintenance,
    int MakeReady,
    IReadOnlyList<DashboardLeaseRow> ExpiringLeases,
    IReadOnlyList<DashboardMaintenanceRow> OpenWorkOrders,
    IReadOnlyList<DelinquencyRow> Delinquencies,
    IReadOnlyList<DashboardWarrantyRow> ExpiringWarranties,
    DateTime GeneratedUtc);

public sealed record DashboardLeaseRow(
    Guid LeaseId,
    string UnitNumber,
    string TenantName,
    DateTime EndUtc,
    int DaysRemaining);

public sealed record DashboardMaintenanceRow(
    Guid Id,
    string UnitNumber,
    string Priority,
    string Status,
    string Description);

public sealed record DashboardWarrantyRow(
    Guid AssetId,
    Guid UnitId,
    string UnitNumber,
    string AssetLabel,
    DateOnly WarrantyEnd,
    int DaysRemaining);
