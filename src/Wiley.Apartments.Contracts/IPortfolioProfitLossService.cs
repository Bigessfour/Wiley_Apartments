namespace Wiley.Apartments.Contracts;

public enum ProfitLossPeriod
{
    Month = 0,
    YearToDate = 1,
    Year = 2
}

public interface IPortfolioProfitLossService
{
    Task<PortfolioProfitLossReport> GetAsync(
        ProfitLossPeriod period,
        DateTime? asOfUtc = null,
        CancellationToken cancellationToken = default);
}

public sealed record PortfolioProfitLossReport(
    ProfitLossPeriod Period,
    DateTime RangeStartUtc,
    DateTime RangeEndUtc,
    decimal TotalIncome,
    decimal TotalExpense,
    decimal NetIncome,
    IReadOnlyList<UnitProfitLossRow> ByUnit,
    IReadOnlyList<PeriodProfitLossPoint> Series);

public sealed record UnitProfitLossRow(
    Guid? UnitId,
    string UnitLabel,
    decimal Income,
    decimal Expense,
    decimal Net);

public sealed record PeriodProfitLossPoint(
    string Label,
    decimal Income,
    decimal Expense,
    decimal Net);
