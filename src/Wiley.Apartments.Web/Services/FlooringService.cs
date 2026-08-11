using Microsoft.EntityFrameworkCore;
using Wiley.Apartments.Contracts;
using Wiley.Apartments.Domain;
using Wiley.Apartments.Web.Data;

namespace Wiley.Apartments.Web.Services;

public sealed class FlooringService(ApartmentsDbContext db, ILogger<FlooringService> logger) : IFlooringService
{
    private readonly ApartmentsDbContext _db = db;
    private readonly ILogger<FlooringService> _logger = logger;

    public async Task<IReadOnlyList<Flooring>> GetByUnitIdAsync(
        Guid unitId,
        CancellationToken cancellationToken = default) =>
        await _db.Floorings
            .AsNoTracking()
            .Where(f => f.UnitId == unitId)
            .OrderByDescending(f => f.InstallDate)
            .ThenByDescending(f => f.ReplacedDate)
            .ToListAsync(cancellationToken);

    public async Task<Flooring?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _db.Floorings.FindAsync([id], cancellationToken);

    public async Task<Flooring> CreateAsync(Flooring flooring, CancellationToken cancellationToken = default)
    {
        ValidateFlooring(flooring);
        await EnsureUnitExists(flooring.UnitId, cancellationToken);

        flooring.Id = Guid.NewGuid();
        _db.Floorings.Add(flooring);
        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation(
            "Created flooring {FlooringId} ({Type}) for unit {UnitId}.",
            flooring.Id,
            flooring.Type,
            flooring.UnitId);
        return flooring;
    }

    public async Task<Flooring> UpdateAsync(Flooring flooring, CancellationToken cancellationToken = default)
    {
        ValidateFlooring(flooring);

        var existing = await _db.Floorings.FindAsync([flooring.Id], cancellationToken)
            ?? throw new InvalidOperationException($"Flooring {flooring.Id} was not found.");

        existing.Type = flooring.Type.Trim();
        existing.InstallDate = flooring.InstallDate;
        existing.Condition = flooring.Condition.Trim();
        existing.ReplacedDate = flooring.ReplacedDate;
        existing.Notes = flooring.Notes;

        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Updated flooring {FlooringId}.", existing.Id);
        return existing;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var flooring = await _db.Floorings.FindAsync([id], cancellationToken)
            ?? throw new InvalidOperationException($"Flooring {id} was not found.");

        _db.Floorings.Remove(flooring);
        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Deleted flooring {FlooringId}.", id);
    }

    private async Task EnsureUnitExists(Guid unitId, CancellationToken cancellationToken)
    {
        if (!await _db.Units.AnyAsync(u => u.Id == unitId, cancellationToken))
        {
            throw new InvalidOperationException($"Unit {unitId} was not found.");
        }
    }

    private static void ValidateFlooring(Flooring flooring)
    {
        if (flooring.UnitId == Guid.Empty)
        {
            throw new ArgumentException("UnitId is required.", nameof(flooring));
        }

        if (string.IsNullOrWhiteSpace(flooring.Type))
        {
            throw new ArgumentException("Flooring type/material is required.", nameof(flooring));
        }

        if (flooring.ReplacedDate is not null
            && flooring.InstallDate is not null
            && flooring.ReplacedDate < flooring.InstallDate)
        {
            throw new ArgumentException("Replaced date cannot be before install date.", nameof(flooring));
        }

        flooring.Type = flooring.Type.Trim();
        flooring.Condition = flooring.Condition.Trim();
    }
}
