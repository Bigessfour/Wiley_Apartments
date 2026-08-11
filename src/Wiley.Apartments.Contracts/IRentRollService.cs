namespace Wiley.Apartments.Contracts;

public interface IRentRollService
{
    Task<IReadOnlyList<RentRollRow>> GetRentRollAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DelinquencyRow>> GetDelinquencyAsync(CancellationToken cancellationToken = default);
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
