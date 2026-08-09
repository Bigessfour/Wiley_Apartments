using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Wiley.Apartments.Contracts;
using Wiley.Apartments.Domain;
using Wiley.Apartments.Web.Configuration;
using Wiley.Apartments.Web.Data;

namespace Wiley.Apartments.Web.Services;

public sealed class UnitService : IUnitService
{
    private readonly ApartmentsDbContext _db;
    private readonly ClerkSuiteOptions _options;
    private readonly ILogger<UnitService> _logger;

    public UnitService(
        ApartmentsDbContext db,
        IOptions<ClerkSuiteOptions> options,
        ILogger<UnitService> logger)
    {
        _db = db;
        _options = options.Value;
        _logger = logger;
    }

    public int MaxUnits => _options.MaxUnits;

    public Task<int> CountAsync(CancellationToken cancellationToken = default) =>
        _db.Units.CountAsync(cancellationToken);

    public async Task<IReadOnlyList<Unit>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await _db.Units
            .AsNoTracking()
            .OrderBy(u => u.Number)
            .ToListAsync(cancellationToken);

    public async Task<Unit?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _db.Units.FindAsync([id], cancellationToken);

    public async Task<Unit> CreateAsync(Unit unit, CancellationToken cancellationToken = default)
    {
        ValidateUnit(unit);

        if (await _db.Units.CountAsync(cancellationToken) >= MaxUnits)
        {
            throw new InvalidOperationException($"Cannot add more than {MaxUnits} units.");
        }

        if (await _db.Units.AnyAsync(u => u.Number == unit.Number, cancellationToken))
        {
            throw new InvalidOperationException($"Unit number '{unit.Number}' already exists.");
        }

        unit.Id = Guid.NewGuid();
        _db.Units.Add(unit);
        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Created unit {UnitNumber} ({UnitId}).", unit.Number, unit.Id);
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

        existing.Number = unit.Number.Trim();
        existing.SqFt = unit.SqFt;
        existing.Beds = unit.Beds;
        existing.Baths = unit.Baths;
        existing.Status = unit.Status;
        existing.Notes = unit.Notes;
        existing.CurrentTenantId = unit.CurrentTenantId;

        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Updated unit {UnitNumber} ({UnitId}).", existing.Number, existing.Id);
        return existing;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var unit = await _db.Units.FindAsync([id], cancellationToken)
            ?? throw new InvalidOperationException($"Unit {id} was not found.");

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

        unit.Number = unit.Number.Trim();
    }
}
