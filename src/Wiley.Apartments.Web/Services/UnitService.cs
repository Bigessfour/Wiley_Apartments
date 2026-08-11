using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Wiley.Apartments.Contracts;
using Wiley.Apartments.Domain;
using Wiley.Apartments.Web.Configuration;
using Wiley.Apartments.Web.Data;

namespace Wiley.Apartments.Web.Services;

public sealed class UnitService(
    ApartmentsDbContext db,
    IOptions<ClerkSuiteOptions> options,
    ILogger<UnitService> logger) : IUnitService
{
    public const string CommunityCenterNumber = "CC";

    private readonly ApartmentsDbContext _db = db;
    private readonly ClerkSuiteOptions _options = options.Value;
    private readonly ILogger<UnitService> _logger = logger;

    public int MaxUnits => _options.MaxUnits;

    public Task<int> CountAsync(CancellationToken cancellationToken = default) =>
        _db.Units.CountAsync(u => !u.IsFacility, cancellationToken);

    public async Task<IReadOnlyList<Unit>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await _db.Units
            .AsNoTracking()
            .OrderBy(u => u.IsFacility ? 1 : 0)
            .ThenBy(u => u.Number)
            .ToListAsync(cancellationToken);

    public async Task<Unit?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _db.Units.FindAsync([id], cancellationToken);

    public async Task<Unit?> GetFacilityAsync(CancellationToken cancellationToken = default)
    {
        var cc = await _db.Units
            .AsNoTracking()
            .FirstOrDefaultAsync(
                u => u.IsFacility && u.Number == CommunityCenterNumber,
                cancellationToken);
        if (cc is not null)
        {
            return cc;
        }

        return await _db.Units
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.IsFacility, cancellationToken);
    }

    public async Task<Unit> CreateAsync(Unit unit, CancellationToken cancellationToken = default)
    {
        ValidateUnit(unit);

        if (unit.IsFacility)
        {
            if (unit.Number != CommunityCenterNumber
                && await _db.Units.AnyAsync(u => u.IsFacility, cancellationToken))
            {
                throw new InvalidOperationException(
                    "A facility unit already exists. Use Number \"CC\" for Community Center only.");
            }
        }
        else if (MaxUnits > 0)
        {
            var residentialCount = await _db.Units.CountAsync(u => !u.IsFacility, cancellationToken);
            if (residentialCount >= MaxUnits)
            {
                throw new InvalidOperationException($"Cannot add more than {MaxUnits} residential units.");
            }
        }

        if (await _db.Units.AnyAsync(u => u.Number == unit.Number, cancellationToken))
        {
            throw new InvalidOperationException($"Unit number '{unit.Number}' already exists.");
        }

        unit.Id = Guid.NewGuid();
        if (unit.Number == CommunityCenterNumber)
        {
            unit.IsFacility = true;
        }

        _db.Units.Add(unit);
        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation(
            "Created unit {UnitNumber} ({UnitId}) facility={IsFacility} rent={MonthlyRent} deposit={SecurityDeposit} handicap={IsHandicapAccessible} leaseTerm={LeaseTerm}.",
            unit.Number,
            unit.Id,
            unit.IsFacility,
            unit.MonthlyRent,
            unit.SecurityDeposit,
            unit.IsHandicapAccessible,
            unit.LeaseTerm);
        return unit;
    }

    public async Task<Unit> UpdateAsync(Unit unit, CancellationToken cancellationToken = default)
    {
        ValidateUnit(unit);

        var existing = await _db.Units.FindAsync([unit.Id], cancellationToken)
            ?? throw new InvalidOperationException($"Unit {unit.Id} was not found.");

        if (await _db.Units.AnyAsync(u => u.Id != unit.Id && u.Number == unit.Number, cancellationToken))
        {
            throw new InvalidOperationException($"Unit number '{unit.Number}' already exists.");
        }

        // Facility flag is system-managed: preserve for seeded CC; allow Number "CC" to force facility.
        var isFacility = existing.IsFacility || unit.IsFacility || unit.Number == CommunityCenterNumber;
        if (existing.IsFacility && unit.Number != CommunityCenterNumber && unit.Number != existing.Number)
        {
            // Keep facility identity if renumbering away from CC is attempted without clearing flag.
            isFacility = true;
        }

        existing.Number = unit.Number.Trim();
        existing.SqFt = unit.SqFt;
        existing.Beds = unit.Beds;
        existing.Baths = unit.Baths;
        existing.Status = unit.Status;
        existing.Notes = unit.Notes;
        existing.CurrentTenantId = unit.CurrentTenantId;
        existing.MonthlyRent = unit.MonthlyRent;
        existing.SecurityDeposit = unit.SecurityDeposit;
        existing.IsHandicapAccessible = unit.IsHandicapAccessible;
        existing.LeaseTerm = string.IsNullOrWhiteSpace(unit.LeaseTerm)
            ? string.Empty
            : unit.LeaseTerm.Trim();
        existing.IsFacility = isFacility;

        _db.Entry(existing).Property(e => e.RowVersion).OriginalValue = unit.RowVersion;
        ConcurrencyHelper.BumpRowVersion(existing);

        await ConcurrencyHelper.SaveChangesOrThrowAsync(_db, "Unit", cancellationToken);
        _logger.LogInformation(
            "Updated unit {UnitNumber} ({UnitId}) status={Status} rent={MonthlyRent} deposit={SecurityDeposit} handicap={IsHandicapAccessible} leaseTerm={LeaseTerm} currentTenant={CurrentTenantId}.",
            existing.Number,
            existing.Id,
            existing.Status,
            existing.MonthlyRent,
            existing.SecurityDeposit,
            existing.IsHandicapAccessible,
            existing.LeaseTerm,
            existing.CurrentTenantId);
        return existing;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var unit = await _db.Units.FindAsync([id], cancellationToken)
            ?? throw new InvalidOperationException($"Unit {id} was not found.");

        if (unit.IsFacility || unit.Number == CommunityCenterNumber)
        {
            throw new InvalidOperationException(
                "Community Center facility unit cannot be deleted. Soft-close by status/notes instead.");
        }

        if (unit.Status == UnitStatus.Occupied)
        {
            throw new InvalidOperationException("Cannot delete an occupied unit.");
        }

        _db.Units.Remove(unit);
        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Deleted unit {UnitNumber} ({UnitId}).", unit.Number, unit.Id);
    }

    private static void ValidateUnit(Unit unit)
    {
        if (string.IsNullOrWhiteSpace(unit.Number))
        {
            throw new ArgumentException("Unit number is required.", nameof(unit));
        }

        if (unit.SqFt < 0)
        {
            throw new ArgumentException("Square footage cannot be negative.", nameof(unit));
        }

        if (unit.Beds < 0)
        {
            throw new ArgumentException("Bed count cannot be negative.", nameof(unit));
        }

        if (unit.Baths < 0)
        {
            throw new ArgumentException("Bath count cannot be negative.", nameof(unit));
        }

        if (unit.MonthlyRent < 0)
        {
            throw new ArgumentException("Monthly rent cannot be negative.", nameof(unit));
        }

        if (unit.SecurityDeposit < 0)
        {
            throw new ArgumentException("Security deposit cannot be negative.", nameof(unit));
        }

        unit.Number = unit.Number.Trim();
        unit.LeaseTerm = string.IsNullOrWhiteSpace(unit.LeaseTerm)
            ? string.Empty
            : unit.LeaseTerm.Trim();
    }
}
