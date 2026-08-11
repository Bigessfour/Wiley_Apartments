using Microsoft.EntityFrameworkCore;
using Wiley.Apartments.Contracts;
using Wiley.Apartments.Domain;
using Wiley.Apartments.Web.Data;

namespace Wiley.Apartments.Web.Services;

public sealed class FacilityInventoryService(
    ApartmentsDbContext db,
    ILogger<FacilityInventoryService> logger) : IFacilityInventoryService
{
    private readonly ApartmentsDbContext _db = db;
    private readonly ILogger<FacilityInventoryService> _logger = logger;

    public async Task<IReadOnlyList<FacilityInventoryItem>> ListAsync(
        Guid unitId,
        FacilityInventoryCategory? category = null,
        bool includeZeroQuantity = true,
        CancellationToken cancellationToken = default)
    {
        var q = _db.FacilityInventoryItems.AsNoTracking()
            .Where(i => i.UnitId == unitId && !i.IsDeleted);
        if (category is FacilityInventoryCategory cat)
        {
            q = q.Where(i => i.Category == cat);
        }

        if (!includeZeroQuantity)
        {
            q = q.Where(i => i.Quantity > 0);
        }

        return await q
            .OrderBy(i => i.Category)
            .ThenBy(i => i.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<FacilityInventoryItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _db.FacilityInventoryItems.FirstOrDefaultAsync(i => i.Id == id, cancellationToken);

    public async Task<FacilityInventoryItem> CreateAsync(
        FacilityInventoryItem item,
        CancellationToken cancellationToken = default)
    {
        await EnsureFacilityUnitAsync(item.UnitId, cancellationToken);
        Validate(item);
        item.Id = Guid.NewGuid();
        item.IsDeleted = false;
        item.RowVersion = Guid.NewGuid();
        _db.FacilityInventoryItems.Add(item);
        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Created facility inventory {Id} {Name} qty {Qty}.", item.Id, item.Name, item.Quantity);
        return item;
    }

    public async Task<FacilityInventoryItem> UpdateAsync(
        FacilityInventoryItem item,
        CancellationToken cancellationToken = default)
    {
        await EnsureFacilityUnitAsync(item.UnitId, cancellationToken);
        Validate(item);
        var existing = await _db.FacilityInventoryItems.FindAsync([item.Id], cancellationToken)
            ?? throw new InvalidOperationException($"Facility inventory item {item.Id} was not found.");
        if (existing.IsDeleted)
        {
            throw new InvalidOperationException("Cannot update a soft-deleted inventory item.");
        }

        existing.Category = item.Category;
        existing.Name = item.Name.Trim();
        existing.Quantity = item.Quantity;
        existing.Condition = item.Condition.Trim();
        existing.Location = Trim(item.Location, 128);
        existing.Serial = Trim(item.Serial, 128);
        existing.Notes = Trim(item.Notes, 2000);
        existing.UnitId = item.UnitId;

        _db.Entry(existing).Property(e => e.RowVersion).OriginalValue = item.RowVersion;
        ConcurrencyHelper.BumpRowVersion(existing);
        await ConcurrencyHelper.SaveChangesOrThrowAsync(_db, "FacilityInventoryItem", cancellationToken);
        _logger.LogInformation(
            "Updated facility inventory {Id} name={Name} qty={Qty} condition={Condition}.",
            existing.Id, existing.Name, existing.Quantity, existing.Condition);
        return existing;
    }

    public async Task SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var item = await _db.FacilityInventoryItems.FindAsync([id], cancellationToken)
            ?? throw new InvalidOperationException($"Facility inventory item {id} was not found.");
        if (item.IsDeleted)
        {
            _logger.LogInformation("Facility inventory {Id} already soft-deleted.", id);
            return;
        }

        item.IsDeleted = true;
        ConcurrencyHelper.BumpRowVersion(item);
        await ConcurrencyHelper.SaveChangesOrThrowAsync(_db, "FacilityInventoryItem", cancellationToken);
        _logger.LogInformation("Soft-deleted facility inventory {Id} name={Name}.", id, item.Name);
    }

    private async Task EnsureFacilityUnitAsync(Guid unitId, CancellationToken cancellationToken)
    {
        var unit = await _db.Units.AsNoTracking().FirstOrDefaultAsync(u => u.Id == unitId, cancellationToken)
            ?? throw new InvalidOperationException($"Unit {unitId} was not found.");
        if (!unit.IsFacility)
        {
            throw new InvalidOperationException("Facility inventory requires the Community Center facility unit.");
        }
    }

    private static void Validate(FacilityInventoryItem item)
    {
        if (string.IsNullOrWhiteSpace(item.Name))
        {
            throw new ArgumentException("Name is required.");
        }

        if (item.Quantity < 0)
        {
            throw new ArgumentException("Quantity cannot be negative.");
        }

        if (string.IsNullOrWhiteSpace(item.Condition))
        {
            item.Condition = "Good";
        }

        item.Name = item.Name.Trim();
        item.Condition = item.Condition.Trim();
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
}
