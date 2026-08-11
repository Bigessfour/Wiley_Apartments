using Wiley.Apartments.Domain;

namespace Wiley.Apartments.Contracts;

public interface IUnitOperatingCostService
{
    Task<UnitOperatingCost?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UnitOperatingCost>> QueryAsync(
        Guid? unitId = null,
        OperatingCostCategory? category = null,
        DateTime? rangeStartUtc = null,
        DateTime? rangeEndUtc = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UnitOperatingCostSum>> SumByUnitAsync(
        DateTime? rangeStartUtc = null,
        DateTime? rangeEndUtc = null,
        CancellationToken cancellationToken = default);

    Task<UnitOperatingCost> CreateAsync(
        OperatingCostCategory category,
        decimal amount,
        DateTime incurredUtc,
        Guid? unitId = null,
        string? vendor = null,
        string? notes = null,
        Guid? maintenanceRequestId = null,
        CancellationToken cancellationToken = default);

    Task<UnitOperatingCost> UpdateAsync(
        Guid id,
        OperatingCostCategory category,
        decimal amount,
        DateTime incurredUtc,
        Guid? unitId = null,
        string? vendor = null,
        string? notes = null,
        CancellationToken cancellationToken = default);

    Task SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

/// <summary><see cref="UnitId"/> null means building-wide CommonUpkeep totals.</summary>
public sealed record UnitOperatingCostSum(Guid? UnitId, decimal Total);
