using Microsoft.EntityFrameworkCore;
using Wiley.Apartments.Contracts;
using Wiley.Apartments.Domain;
using Wiley.Apartments.Web.Data;

namespace Wiley.Apartments.Web.Services;

public sealed class PortfolioProfitLossService(
    ApartmentsDbContext db,
    IDateTimeService clock,
    ILogger<PortfolioProfitLossService> logger) : IPortfolioProfitLossService
{
    private readonly ApartmentsDbContext _db = db;
    private readonly IDateTimeService _clock = clock;
    private readonly ILogger<PortfolioProfitLossService> _logger = logger;

    public async Task<PortfolioProfitLossReport> GetAsync(
        ProfitLossPeriod period,
        DateTime? asOfUtc = null,
        CancellationToken cancellationToken = default)
    {
        var asOf = EnsureUtc(asOfUtc ?? _clock.UtcNow);
        var (start, end) = ResolveRange(period, asOf);

        var payments = await _db.LedgerEntries.AsNoTracking()
            .Where(e => !e.IsDeleted
                && e.EntryType == LedgerEntryType.Payment
                && !e.IsDeposit
                && e.DateUtc >= start
                && e.DateUtc <= end)
            .ToListAsync(cancellationToken);

        var costs = await _db.UnitOperatingCosts.AsNoTracking()
            .Include(c => c.Unit)
            .Where(c => !c.IsDeleted && c.IncurredUtc >= start && c.IncurredUtc <= end)
            .ToListAsync(cancellationToken);

        var units = await _db.Units.AsNoTracking()
            .OrderBy(u => u.Number)
            .ToListAsync(cancellationToken);

        var incomeByUnit = payments
            .GroupBy(p => p.UnitId)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Amount));

        var expenseByUnit = costs
            .Where(c => c.UnitId is not null)
            .GroupBy(c => c.UnitId!.Value)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Amount));

        var buildingExpense = costs.Where(c => c.UnitId is null).Sum(c => c.Amount);
        var allocatedCommon = units.Count == 0 ? 0m : buildingExpense / units.Count;

        var byUnit = new List<UnitProfitLossRow>();
        foreach (var unit in units)
        {
            incomeByUnit.TryGetValue(unit.Id, out var income);
            expenseByUnit.TryGetValue(unit.Id, out var expense);
            expense += allocatedCommon;
            byUnit.Add(new UnitProfitLossRow(
                unit.Id,
                $"Unit {unit.Number}",
                income,
                expense,
                income - expense));
        }

        if (buildingExpense > 0)
        {
            byUnit.Add(new UnitProfitLossRow(
                null,
                "Common upkeep (allocated)",
                0m,
                buildingExpense,
                -buildingExpense));
        }

        var totalIncome = payments.Sum(p => p.Amount);
        var totalExpense = costs.Sum(c => c.Amount);

        var series = BuildSeries(period, start, end, payments, costs);

        _logger.LogInformation(
            "Portfolio P/L {Period}: income {Income:C}, expense {Expense:C}, net {Net:C} ({UnitCount} unit rows).",
            period,
            totalIncome,
            totalExpense,
            totalIncome - totalExpense,
            byUnit.Count);

        return new PortfolioProfitLossReport(
            period,
            start,
            end,
            totalIncome,
            totalExpense,
            totalIncome - totalExpense,
            byUnit,
            series);
    }

    private static List<PeriodProfitLossPoint> BuildSeries(
        ProfitLossPeriod period,
        DateTime start,
        DateTime end,
        List<LedgerEntry> payments,
        List<UnitOperatingCost> costs)
    {
        if (period == ProfitLossPeriod.Month)
        {
            // Daily points within the month
            var days = new List<PeriodProfitLossPoint>();
            for (var d = start.Date; d <= end.Date; d = d.AddDays(1))
            {
                var dayEnd = d.AddDays(1);
                var income = payments.Where(p => p.DateUtc >= d && p.DateUtc < dayEnd).Sum(p => p.Amount);
                var expense = costs.Where(c => c.IncurredUtc >= d && c.IncurredUtc < dayEnd).Sum(c => c.Amount);
                days.Add(new PeriodProfitLossPoint(d.ToString("MM/dd"), income, expense, income - expense));
            }

            return days;
        }

        // Monthly buckets for YTD / Year
        var points = new List<PeriodProfitLossPoint>();
        for (var cursor = new DateTime(start.Year, start.Month, 1, 0, 0, 0, DateTimeKind.Utc);
             cursor <= end;
             cursor = cursor.AddMonths(1))
        {
            var monthEnd = cursor.AddMonths(1);
            var income = payments.Where(p => p.DateUtc >= cursor && p.DateUtc < monthEnd).Sum(p => p.Amount);
            var expense = costs.Where(c => c.IncurredUtc >= cursor && c.IncurredUtc < monthEnd).Sum(c => c.Amount);
            points.Add(new PeriodProfitLossPoint(cursor.ToString("MMM yyyy"), income, expense, income - expense));
        }

        return points;
    }

    private static (DateTime Start, DateTime End) ResolveRange(ProfitLossPeriod period, DateTime asOf)
    {
        return period switch
        {
            ProfitLossPeriod.Month => (
                new DateTime(asOf.Year, asOf.Month, 1, 0, 0, 0, DateTimeKind.Utc),
                asOf),
            ProfitLossPeriod.YearToDate => (
                new DateTime(asOf.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                asOf),
            ProfitLossPeriod.Year => (
                new DateTime(asOf.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(asOf.Year, 12, 31, 23, 59, 59, DateTimeKind.Utc)),
            _ => (asOf.AddMonths(-1), asOf)
        };
    }

    private static DateTime EnsureUtc(DateTime value) =>
        value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
}
