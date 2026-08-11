using Microsoft.EntityFrameworkCore;
using Wiley.Apartments.Contracts;
using Wiley.Apartments.Domain;
using Wiley.Apartments.Web.Data;

namespace Wiley.Apartments.Web.Services;

public sealed class UnitOperatingCostService : IUnitOperatingCostService
{
    private readonly ApartmentsDbContext _db;
    private readonly ILogger<UnitOperatingCostService> _logger;

    public UnitOperatingCostService(ApartmentsDbContext db, ILogger<UnitOperatingCostService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<UnitOperatingCost?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _db.UnitOperatingCosts
            .AsNoTracking()
            .Include(c => c.Unit)
            .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted, cancellationToken);

    public async Task<IReadOnlyList<UnitOperatingCost>> QueryAsync(
        Guid? unitId = null,
        OperatingCostCategory? category = null,
        DateTime? rangeStartUtc = null,
        DateTime? rangeEndUtc = null,
        CancellationToken cancellationToken = default)
    {
        var query = _db.UnitOperatingCosts
            .AsNoTracking()
            .Include(c => c.Unit)
            .Where(c => !c.IsDeleted);

        if (unitId is Guid uid)
        {
            query = query.Where(c => c.UnitId == uid);
        }

        if (category is OperatingCostCategory cat)
        {
            query = query.Where(c => c.Category == cat);
        }

        if (rangeStartUtc is DateTime rs)
        {
            query = query.Where(c => c.IncurredUtc >= EnsureUtc(rs));
        }

        if (rangeEndUtc is DateTime re)
        {
            query = query.Where(c => c.IncurredUtc <= EnsureUtc(re));
        }

        return await query
            .OrderByDescending(c => c.IncurredUtc)
            .ThenBy(c => c.Category)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<UnitOperatingCostSum>> SumByUnitAsync(
        DateTime? rangeStartUtc = null,
        DateTime? rangeEndUtc = null,
        CancellationToken cancellationToken = default)
    {
        var costs = await QueryAsync(
            rangeStartUtc: rangeStartUtc,
            rangeEndUtc: rangeEndUtc,
            cancellationToken: cancellationToken);
        return costs
            .GroupBy(c => c.UnitId)
            .Select(g => new UnitOperatingCostSum(g.Key, g.Sum(c => c.Amount)))
            .OrderBy(s => s.UnitId.HasValue ? 0 : 1)
            .ThenBy(s => s.UnitId)
            .ToList();
    }

    public async Task<UnitOperatingCost> CreateAsync(
        OperatingCostCategory category,
        decimal amount,
        DateTime incurredUtc,
        Guid? unitId = null,
        string? vendor = null,
        string? notes = null,
        Guid? maintenanceRequestId = null,
        CancellationToken cancellationToken = default)
    {
        ValidateCategoryUnit(category, unitId);
        await EnsureUnitExistsAsync(unitId, cancellationToken);

        var cost = new UnitOperatingCost
        {
            Id = Guid.NewGuid(),
            Category = category,
            UnitId = unitId,
            Amount = RequirePositive(amount),
            IncurredUtc = EnsureUtc(incurredUtc),
            Vendor = Trim(vendor, 256),
            Notes = Trim(notes, 2000),
            MaintenanceRequestId = maintenanceRequestId,
            IsDeleted = false
        };
        _db.UnitOperatingCosts.Add(cost);
        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Created ops cost {Id} {Category} {Amount}.", cost.Id, category, cost.Amount);
        return (await GetByIdAsync(cost.Id, cancellationToken))!;
    }

    public async Task<UnitOperatingCost> UpdateAsync(
        Guid id,
        OperatingCostCategory category,
        decimal amount,
        DateTime incurredUtc,
        Guid? unitId = null,
        string? vendor = null,
        string? notes = null,
        CancellationToken cancellationToken = default)
    {
        ValidateCategoryUnit(category, unitId);
        await EnsureUnitExistsAsync(unitId, cancellationToken);

        var cost = await _db.UnitOperatingCosts
                       .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted, cancellationToken)
                   ?? throw new InvalidOperationException($"Operating cost {id} was not found.");

        cost.Category = category;
        cost.UnitId = unitId;
        cost.Amount = RequirePositive(amount);
        cost.IncurredUtc = EnsureUtc(incurredUtc);
        cost.Vendor = Trim(vendor, 256);
        cost.Notes = Trim(notes, 2000);
        await _db.SaveChangesAsync(cancellationToken);
        return (await GetByIdAsync(cost.Id, cancellationToken))!;
    }

    public async Task SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var cost = await _db.UnitOperatingCosts
                       .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted, cancellationToken)
                   ?? throw new InvalidOperationException($"Operating cost {id} was not found.");
        cost.IsDeleted = true;
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureUnitExistsAsync(Guid? unitId, CancellationToken cancellationToken)
    {
        if (unitId is Guid uid &&
            !await _db.Units.AnyAsync(u => u.Id == uid, cancellationToken))
        {
            throw new InvalidOperationException($"Unit {uid} was not found.");
        }
    }

    private static void ValidateCategoryUnit(OperatingCostCategory category, Guid? unitId)
    {
        if (unitId is null && category != OperatingCostCategory.CommonUpkeep)
        {
            throw new ArgumentException("Unit is required unless category is CommonUpkeep.");
        }
    }

    private static decimal RequirePositive(decimal amount)
    {
        if (amount <= 0)
        {
            throw new ArgumentException("Amount must be greater than zero.", nameof(amount));
        }

        return decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
    }

    private static string? Trim(string? value, int max)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var t = value.Trim();
        return t.Length > max ? t[..max] : t;
    }

    private static DateTime EnsureUtc(DateTime value) =>
        value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
}
