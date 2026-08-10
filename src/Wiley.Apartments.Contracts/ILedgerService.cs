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
        bool isDeposit = false,
        CancellationToken cancellationToken = default);

    Task<LedgerEntry> PostPaymentAsync(
        Guid tenantId,
        Guid unitId,
        decimal amount,
        DateTime dateUtc,
        PaymentMethod method,
        Guid? leaseId = null,
        string? notes = null,
        bool isDeposit = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Records a security-deposit payment: ensures a matching deposit charge exists for the gap,
    /// then posts the payment (both flagged <c>IsDeposit</c>).
    /// </summary>
    Task<LedgerEntry> PostDepositPaymentAsync(
        Guid tenantId,
        Guid unitId,
        decimal amount,
        DateTime dateUtc,
        PaymentMethod method,
        string? notes = null,
        CancellationToken cancellationToken = default);

    Task<DepositSummary> GetDepositSummaryAsync(
        Guid tenantId,
        Guid? unitId = null,
        CancellationToken cancellationToken = default);

    Task SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default);

    Task<int> ApplyLateFeesAsync(
        DateTime? asOfUtc = null,
        CancellationToken cancellationToken = default);

    Task<int> PostMonthlyRentChargesAsync(
        DateTime? asOfUtc = null,
        CancellationToken cancellationToken = default);
}

/// <summary>Ledger row with running balance after this line (Charge +, Payment −).</summary>
public sealed record LedgerLine(
    LedgerEntry Entry,
    decimal RunningBalance);

/// <summary>Security deposit status for a tenant (optionally scoped to a unit).</summary>
public sealed record DepositSummary(
    Guid TenantId,
    Guid? UnitId,
    string? UnitNumber,
    decimal RequiredAmount,
    decimal ChargedAmount,
    decimal PaidAmount,
    decimal HeldAmount,
    decimal StillDue,
    string StatusLabel,
    DateTime? LastDepositPaymentUtc,
    string? LastDepositNotes);
