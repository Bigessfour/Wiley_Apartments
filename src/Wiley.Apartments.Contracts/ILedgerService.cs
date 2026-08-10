using Wiley.Apartments.Domain;

namespace Wiley.Apartments.Contracts;

public interface ILedgerService
{
    Task<LedgerEntry?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LedgerLine>> GetLedgerAsync(
        Guid? tenantId = null,
        Guid? unitId = null,
        CancellationToken cancellationToken = default);

    Task<decimal> GetBalanceAsync(
        Guid tenantId,
        Guid? unitId = null,
        CancellationToken cancellationToken = default);

    Task<LedgerEntry> PostChargeAsync(
        Guid tenantId,
        Guid unitId,
        decimal amount,
        DateTime dateUtc,
        Guid? leaseId = null,
        string? notes = null,
        bool isLateFee = false,
        CancellationToken cancellationToken = default);

    Task<LedgerEntry> PostPaymentAsync(
        Guid tenantId,
        Guid unitId,
        decimal amount,
        DateTime dateUtc,
        PaymentMethod method,
        Guid? leaseId = null,
        string? notes = null,
        CancellationToken cancellationToken = default);

    Task SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// When late fees are enabled, posts one late-fee charge per tenant/unit with positive balance
    /// that has at least one non-late charge past its due window (charge date + grace days)
    /// and no late fee already in the as-of calendar month.
    /// </summary>
    /// <returns>Number of late-fee charges posted.</returns>
    Task<int> ApplyLateFeesAsync(
        DateTime? asOfUtc = null,
        CancellationToken cancellationToken = default);

    /// <summary>Posts monthly rent charges for active leases missing a rent charge in the target month.</summary>
    /// <returns>Number of rent charges posted.</returns>
    Task<int> PostMonthlyRentChargesAsync(
        DateTime? asOfUtc = null,
        CancellationToken cancellationToken = default);
}

/// <summary>Ledger row with running balance after this line (Charge +, Payment −).</summary>
public sealed record LedgerLine(
    LedgerEntry Entry,
    decimal RunningBalance);
