using Microsoft.EntityFrameworkCore;
using Wiley.Apartments.Contracts;
using Wiley.Apartments.Domain;
using Wiley.Apartments.Web.Data;

namespace Wiley.Apartments.Web.Services;

public sealed class FacilityInspectionService(
    ApartmentsDbContext db,
    IDateTimeService clock,
    ILogger<FacilityInspectionService> logger) : IFacilityInspectionService
{
    private readonly ApartmentsDbContext _db = db;
    private readonly IDateTimeService _clock = clock;
    private readonly ILogger<FacilityInspectionService> _logger = logger;

    public async Task<IReadOnlyList<FacilityInspection>> ListForReservationAsync(
        Guid reservationId,
        CancellationToken cancellationToken = default) =>
        await _db.FacilityInspections.AsNoTracking()
            .Where(i => i.FacilityReservationId == reservationId)
            .OrderByDescending(i => i.InspectedUtc)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<FacilityInspection>> ListRecentAsync(
        int take = 50,
        CancellationToken cancellationToken = default) =>
        await _db.FacilityInspections.AsNoTracking()
            .Include(i => i.FacilityReservation)!
            .ThenInclude(r => r!.FacilityRenter)
            .OrderByDescending(i => i.InspectedUtc)
            .Take(Math.Clamp(take, 1, 500))
            .ToListAsync(cancellationToken);

    public async Task<FacilityInspection> CreateAsync(
        FacilityInspection inspection,
        CancellationToken cancellationToken = default)
    {
        Validate(inspection);
        if (!await _db.FacilityReservations.AnyAsync(
                r => r.Id == inspection.FacilityReservationId && !r.IsDeleted, cancellationToken))
        {
            throw new InvalidOperationException(
                $"Facility reservation {inspection.FacilityReservationId} was not found.");
        }

        inspection.Id = Guid.NewGuid();
        inspection.InspectedUtc = EnsureUtc(
            inspection.InspectedUtc == default ? _clock.UtcNow : inspection.InspectedUtc);
        inspection.RowVersion = Guid.NewGuid();
        _db.FacilityInspections.Add(inspection);
        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation(
            "Created facility inspection {Id} for reservation {ReservationId} satisfactory={Ok}.",
            inspection.Id, inspection.FacilityReservationId, inspection.IsSatisfactory);
        return inspection;
    }

    public async Task<FacilityInspection> UpdateAsync(
        FacilityInspection inspection,
        CancellationToken cancellationToken = default)
    {
        Validate(inspection);
        var existing = await _db.FacilityInspections.FindAsync([inspection.Id], cancellationToken)
            ?? throw new InvalidOperationException($"Facility inspection {inspection.Id} was not found.");

        existing.Type = inspection.Type;
        existing.IsSatisfactory = inspection.IsSatisfactory;
        existing.ChecklistNotes = Trim(inspection.ChecklistNotes, 4000);
        existing.DamageNotes = Trim(inspection.DamageNotes, 4000);
        existing.InspectorUserId = Trim(inspection.InspectorUserId, 256);
        existing.InspectorDisplay = inspection.InspectorDisplay.Trim();
        existing.InspectedUtc = EnsureUtc(inspection.InspectedUtc);

        _db.Entry(existing).Property(e => e.RowVersion).OriginalValue = inspection.RowVersion;
        ConcurrencyHelper.BumpRowVersion(existing);
        await ConcurrencyHelper.SaveChangesOrThrowAsync(_db, "FacilityInspection", cancellationToken);
        _logger.LogInformation(
            "Updated facility inspection {Id} reservation={ReservationId} type={Type} satisfactory={Ok}.",
            existing.Id, existing.FacilityReservationId, existing.Type, existing.IsSatisfactory);
        return existing;
    }

    private static void Validate(FacilityInspection inspection)
    {
        if (string.IsNullOrWhiteSpace(inspection.InspectorDisplay))
        {
            throw new ArgumentException("Inspector name is required.");
        }

        if (!inspection.IsSatisfactory && string.IsNullOrWhiteSpace(inspection.DamageNotes))
        {
            throw new ArgumentException("Damage notes are required when condition is not satisfactory.");
        }

        inspection.InspectorDisplay = inspection.InspectorDisplay.Trim();
        inspection.ChecklistNotes = Trim(inspection.ChecklistNotes, 4000);
        inspection.DamageNotes = Trim(inspection.DamageNotes, 4000);
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
