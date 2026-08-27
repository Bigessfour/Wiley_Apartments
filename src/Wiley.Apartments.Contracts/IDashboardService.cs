namespace Wiley.Apartments.Contracts;

public interface IDashboardService
{
    Task<DashboardSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RentPivotRow>> GetRentPivotRowsAsync(CancellationToken cancellationToken = default);
}

public sealed record DashboardSnapshot(
    int TotalUnits,
    int Occupied,
    int Vacant,
    int Maintenance,
    int MakeReady,
    IReadOnlyList<DashboardLeaseRow> ExpiringLeasesWithin30,
    IReadOnlyList<DashboardLeaseRow> ExpiringLeasesWithin60,
    IReadOnlyList<DashboardMaintenanceRow> OpenWorkOrders,
    IReadOnlyList<DelinquencyRow> Delinquencies,
    IReadOnlyList<DashboardWarrantyRow> ExpiringWarranties,
    IReadOnlyList<DashboardScheduleReminderRow> ScheduleReminders,
    DateTime GeneratedUtc,
    double OccupancyPercent,
    decimal ExpectedRentThisMonth,
    decimal CollectedThisMonth,
    decimal OutstandingBalanceTotal,
    IReadOnlyList<DashboardStatusSlice> UnitStatusSlices,
    IReadOnlyList<DashboardMonthAmount> CollectionByMonth,
    double CollectionRatePercent);

public sealed record DashboardStatusSlice(string Status, int Count);

public sealed record DashboardMonthAmount(string Label, decimal Amount);

public sealed record RentPivotRow(
    string Unit,
    string Year,
    string Month,
    decimal Amount,
    string EntryKind,
    decimal PaymentAmount,
    decimal ChargeAmount);

public sealed record DashboardScheduleReminderRow(
    Guid Id,
    string Title,
    string UnitNumber,
    DateTime ReminderUtc,
    DateTime DueOrStartUtc,
    string Category);

public sealed record DashboardLeaseRow(
    Guid LeaseId,
    string UnitNumber,
    string TenantName,
    DateTime EndUtc,
    int DaysRemaining);

public sealed record DashboardMaintenanceRow(
    Guid Id,
    Guid UnitId,
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
