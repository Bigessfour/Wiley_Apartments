using Microsoft.EntityFrameworkCore;
using Wiley.Apartments.Contracts;
using Wiley.Apartments.Domain;
using Wiley.Apartments.Web.Data;

namespace Wiley.Apartments.Web.Services;

public sealed class OccupancyService(
    ApartmentsDbContext db,
    IDateTimeService clock,
    ILogger<OccupancyService> logger) : IOccupancyService
{
    private readonly ApartmentsDbContext _db = db;
    private readonly IDateTimeService _clock = clock;
    private readonly ILogger<OccupancyService> _logger = logger;

    public async Task<Occupancy?> GetCurrentForUnitAsync(
        Guid unitId,
        CancellationToken cancellationToken = default) =>
        await _db.Occupancies
            .AsNoTracking()
            .Include(o => o.Tenant)
            .Where(o => o.UnitId == unitId && o.EndUtc == null)
            .OrderByDescending(o => o.StartUtc)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<Occupancy>> GetHistoryForUnitAsync(
        Guid unitId,
        CancellationToken cancellationToken = default) =>
        await _db.Occupancies
            .AsNoTracking()
            .Include(o => o.Tenant)
            .Where(o => o.UnitId == unitId)
            .OrderByDescending(o => o.StartUtc)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Occupancy>> GetHistoryForTenantAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default) =>
        await _db.Occupancies
            .AsNoTracking()
            .Include(o => o.Unit)
            .Where(o => o.TenantId == tenantId)
            .OrderByDescending(o => o.StartUtc)
            .ToListAsync(cancellationToken);

    public async Task<Occupancy> StartAsync(
        Guid unitId,
        Guid tenantId,
        DateTime? startUtc = null,
        CancellationToken cancellationToken = default)
    {
        var unit = await _db.Units.FindAsync([unitId], cancellationToken)
            ?? throw new InvalidOperationException($"Unit {unitId} was not found.");
        await _db.Entry(unit).ReloadAsync(cancellationToken);

        var tenant = await _db.Tenants.FindAsync([tenantId], cancellationToken)
            ?? throw new InvalidOperationException($"Tenant {tenantId} was not found.");

        if (unit.IsFacility || unit.Number == UnitService.CommunityCenterNumber)
        {
            throw new InvalidOperationException(
                "Community Center is a facility, not a rental unit. Use CC reservations instead of occupancy.");
        }

        if (tenant.IsDeleted)
        {
            throw new InvalidOperationException("Cannot start occupancy for a soft-deleted tenant.");
        }

        if (await _db.Occupancies.AnyAsync(
                o => o.UnitId == unitId && o.EndUtc == null,
                cancellationToken))
        {
            throw new InvalidOperationException("Unit already has an active occupancy. End it first.");
        }

        if (await _db.Occupancies.AnyAsync(
                o => o.TenantId == tenantId && o.EndUtc == null,
                cancellationToken))
        {
            throw new InvalidOperationException("Tenant already has an active occupancy on another unit.");
        }

        var start = startUtc ?? _clock.UtcNow;
        if (start.Kind == DateTimeKind.Unspecified)
        {
            start = DateTime.SpecifyKind(start, DateTimeKind.Utc);
        }
        else if (start.Kind == DateTimeKind.Local)
        {
            start = start.ToUniversalTime();
        }

        var occupancy = new Occupancy
        {
            Id = Guid.NewGuid(),
            UnitId = unitId,
            TenantId = tenantId,
            StartUtc = start,
            EndUtc = null
        };

        _db.Occupancies.Add(occupancy);
        unit.CurrentTenantId = tenantId;
        unit.Status = UnitStatus.Occupied;
        ConcurrencyHelper.BumpRowVersion(unit);

        await ConcurrencyHelper.SaveChangesOrThrowAsync(_db, "Unit", cancellationToken);
        _logger.LogInformation(
            "Started occupancy {OccupancyId} unit {UnitNumber} tenant {TenantId}.",
            occupancy.Id,
            unit.Number,
            tenantId);

        return (await GetCurrentForUnitAsync(unitId, cancellationToken))!;
    }

    public async Task<Occupancy> EndAsync(
        Guid unitId,
        DateTime? endUtc = null,
        CancellationToken cancellationToken = default)
    {
        var unit = await _db.Units.FindAsync([unitId], cancellationToken)
            ?? throw new InvalidOperationException($"Unit {unitId} was not found.");
        await _db.Entry(unit).ReloadAsync(cancellationToken);

        var occupancy = await _db.Occupancies
            .Where(o => o.UnitId == unitId && o.EndUtc == null)
            .OrderByDescending(o => o.StartUtc)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException("Unit has no active occupancy to end.");

        var end = endUtc ?? _clock.UtcNow;
        if (end.Kind == DateTimeKind.Unspecified)
        {
            end = DateTime.SpecifyKind(end, DateTimeKind.Utc);
        }
        else if (end.Kind == DateTimeKind.Local)
        {
            end = end.ToUniversalTime();
        }

        if (end < occupancy.StartUtc)
        {
            throw new InvalidOperationException("End date cannot be before occupancy start.");
        }

        occupancy.EndUtc = end;
        unit.CurrentTenantId = null;
        if (unit.Status == UnitStatus.Occupied)
        {
            // Between tenants: turnover, not available to rent. Clerk sets Vacant when make-ready is done.
            unit.Status = UnitStatus.MakeReady;
        }

        ConcurrencyHelper.BumpRowVersion(unit);
        await ConcurrencyHelper.SaveChangesOrThrowAsync(_db, "Unit", cancellationToken);
        _logger.LogInformation(
            "Ended occupancy {OccupancyId} unit {UnitNumber}; unit is now make-ready.",
            occupancy.Id,
            unit.Number);

        return await _db.Occupancies
            .AsNoTracking()
            .Include(o => o.Tenant)
            .FirstAsync(o => o.Id == occupancy.Id, cancellationToken);
    }
}
