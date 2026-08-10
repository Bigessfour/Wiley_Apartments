namespace Wiley.Apartments.Contracts;

public interface IDemoDataSeeder
{
    Task<bool> IsDemoLoadedAsync(CancellationToken cancellationToken = default);

    /// <summary>Seeds pseudo resident (24 months) + Community Center rentals. Idempotent unless force=true.</summary>
    Task<DemoSeedResult> SeedAsync(bool force = false, CancellationToken cancellationToken = default);

    Task<DemoValidationReport> ValidateAsync(CancellationToken cancellationToken = default);
}

public sealed record DemoSeedResult(
    bool AlreadyLoaded,
    bool Forced,
    string PrimaryTenantName,
    Guid PrimaryTenantId,
    Guid PrimaryUnitId,
    int CommunityCenterRenters,
    int LedgerEntries,
    int Documents,
    int Maintenance,
    int ScheduleItems,
    string Message);

public sealed record DemoValidationReport(
    bool Pass,
    IReadOnlyList<DemoValidationCheck> Checks);

public sealed record DemoValidationCheck(
    string Area,
    bool Pass,
    string Detail,
    int Count = 0);
