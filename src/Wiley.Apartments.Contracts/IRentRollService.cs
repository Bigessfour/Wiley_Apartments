namespace Wiley.Apartments.Contracts;

public interface IRentRollService
{
    Task<IReadOnlyList<RentRollRow>> GetRentRollAsync(CancellationToken cancellationToken = default);

    /// <param name="occupancy">
    /// Current (default) = open occupants only. Former = ended occupancy with a remaining balance.
    /// All = every tenant/unit pair (mixed — not for dashboard).
    /// </param>
    Task<IReadOnlyList<DelinquencyRow>> GetDelinquencyAsync(
        OccupancyFilter occupancy = OccupancyFilter.Current,
        CancellationToken cancellationToken = default);
}

public sealed record RentRollRow(
    Guid UnitId,
    string UnitNumber,
    string Status,
    Guid? TenantId,
    string? TenantName,
    Guid? LeaseId,
    decimal? Rent,
    decimal Balance);

public sealed record DelinquencyRow(
    Guid UnitId,
    string UnitNumber,
    Guid TenantId,
    string TenantName,
    decimal Balance,
    DateTime? OldestChargeUtc);
