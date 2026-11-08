using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Wiley.Apartments.Domain;
using Wiley.Apartments.Web.Data;

namespace Wiley.Apartments.Web.Services;

/// <summary>
/// Maps landlord operating costs to shared-contracts OperationalEvent[] (Wiley Widget ingest).
/// Read-only; does not change ledger, P/L, or cost CRUD behavior.
/// </summary>
public sealed class SharedContractOperationalEventExportService(
    ApartmentsDbContext db,
    ILogger<SharedContractOperationalEventExportService> logger)
{
    public const string EnterpriseId = "tow-apartments";
    public const string PublisherSystem = "wiley-apartments";
    public const string SchemaVersion = "1.0.0";

    /// <summary>
    /// Builds a bare <c>OperationalEvent[]</c> for POST to Widget
    /// <c>/api/reference-data/events</c> (not a PayloadBatch envelope).
    /// </summary>
    /// <param name="period">Calendar month <c>YYYY-MM</c>.</param>
    public async Task<IReadOnlyList<SharedContractOperationalEventDto>> BuildForPeriodAsync(
        string period,
        CancellationToken cancellationToken = default)
    {
        if (!TryParsePeriod(period, out var startUtc, out var endExclusiveUtc))
        {
            throw new ArgumentException("period must be YYYY-MM.", nameof(period));
        }

        var costs = await db.UnitOperatingCosts
            .AsNoTracking()
            .Include(c => c.Unit)
            .Where(c => !c.IsDeleted
                && c.IncurredUtc >= startUtc
                && c.IncurredUtc < endExclusiveUtc)
            .OrderBy(c => c.IncurredUtc)
            .ThenBy(c => c.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var events = costs.Select(MapCost).ToList();

        logger.LogInformation(
            "Shared-contract export period {Period}: {Count} operating-cost events for {EnterpriseId}.",
            period,
            events.Count,
            EnterpriseId);

        return events;
    }

    private static SharedContractOperationalEventDto MapCost(UnitOperatingCost cost)
    {
        var day = DateTime.SpecifyKind(cost.IncurredUtc, DateTimeKind.Utc).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var unitLabel = cost.Unit is null ? "common" : $"Unit {cost.Unit.Number}";
        var category = cost.Category.ToString();
        var description = string.IsNullOrWhiteSpace(cost.Vendor)
            ? $"{category} — {unitLabel}"
            : $"{cost.Vendor.Trim()} — {category} ({unitLabel})";
        if (!string.IsNullOrWhiteSpace(cost.Notes))
        {
            description = $"{description}: {cost.Notes.Trim()}";
        }

        // Contract enums: source/type are closed. Publisher lives in metadata.
        // type=check = positive outflow (same convention as council check listings).
        var metadata = new Dictionary<string, object?>
        {
            ["enterpriseId"] = EnterpriseId,
            ["publisherSystem"] = PublisherSystem,
            ["costCategory"] = category,
            ["operatingCostId"] = cost.Id.ToString("D"),
            ["unitId"] = cost.UnitId?.ToString("D"),
            ["unitNumber"] = cost.Unit?.Number,
            ["vendor"] = cost.Vendor,
            ["payee"] = cost.Vendor
        };

        return new SharedContractOperationalEventDto(
            SchemaVersion,
            Source: "other",
            Type: "check",
            Date: day,
            Amount: decimal.Round(cost.Amount, 2, MidpointRounding.AwayFromZero),
            Description: description,
            Metadata: metadata);
    }

    internal static bool TryParsePeriod(string period, out DateTime startUtc, out DateTime endExclusiveUtc)
    {
        startUtc = default;
        endExclusiveUtc = default;
        if (string.IsNullOrWhiteSpace(period)
            || !DateTime.TryParseExact(
                period.Trim() + "-01",
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var monthStart))
        {
            return false;
        }

        startUtc = new DateTime(monthStart.Year, monthStart.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        endExclusiveUtc = startUtc.AddMonths(1);
        return true;
    }
}

/// <summary>JSON shape matching shared-contracts OperationalEvent (camelCase).</summary>
public sealed record SharedContractOperationalEventDto(
    string SchemaVersion,
    string Source,
    string Type,
    string Date,
    decimal Amount,
    string Description,
    Dictionary<string, object?> Metadata);
