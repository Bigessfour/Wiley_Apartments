using Microsoft.EntityFrameworkCore;
using Wiley.Apartments.Contracts;
using Wiley.Apartments.Domain;
using Wiley.Apartments.Web.Data;

namespace Wiley.Apartments.Web.Services;

public sealed class FacilityReservationService(
    ApartmentsDbContext db,
    IDateTimeService clock,
    ILogger<FacilityReservationService> logger) : IFacilityReservationService
{
    private readonly ApartmentsDbContext _db = db;
    private readonly IDateTimeService _clock = clock;
    private readonly ILogger<FacilityReservationService> _logger = logger;

    public async Task<IReadOnlyList<FacilityReservation>> ListAsync(
        Guid? unitId = null,
        FacilityReservationStatus? status = null,
        DateTime? fromUtc = null,
        DateTime? toUtc = null,
        Guid? facilityRenterId = null,
        CancellationToken cancellationToken = default)
    {
        var q = BaseQuery();
        if (unitId is Guid uid)
        {
            q = q.Where(r => r.UnitId == uid);
        }

        if (facilityRenterId is Guid rid)
        {
            q = q.Where(r => r.FacilityRenterId == rid);
        }

        if (status is FacilityReservationStatus st)
        {
            q = q.Where(r => r.Status == st);
        }

        if (fromUtc is DateTime from)
        {
            q = q.Where(r => r.EndUtc >= from);
        }

        if (toUtc is DateTime to)
        {
            q = q.Where(r => r.StartUtc <= to);
        }

        return await q
            .OrderByDescending(r => r.StartUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<FacilityReservation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await BaseQuery().FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public async Task<FacilityReservation> CreateAsync(
        FacilityReservation reservation,
        CancellationToken cancellationToken = default)
    {
        await ValidateReservationAsync(reservation, excludeId: null, cancellationToken);
        reservation.Id = Guid.NewGuid();
        reservation.IsDeleted = false;
        reservation.RowVersion = Guid.NewGuid();
        if (reservation.Status == FacilityReservationStatus.Confirmed)
        {
            await EnsureNoConfirmedOverlapAsync(
                reservation.UnitId, reservation.StartUtc, reservation.EndUtc, null, cancellationToken);
        }

        _db.FacilityReservations.Add(reservation);
        await _db.SaveChangesAsync(cancellationToken);

        if (reservation.Status == FacilityReservationStatus.Confirmed)
        {
            await UpsertCalendarAsync(reservation, cancellationToken);
        }

        _logger.LogInformation("Created facility reservation {Id} status {Status}.", reservation.Id, reservation.Status);
        return (await GetByIdAsync(reservation.Id, cancellationToken))!;
    }

    public async Task<FacilityReservation> UpdateAsync(
        FacilityReservation reservation,
        CancellationToken cancellationToken = default)
    {
        var existing = await _db.FacilityReservations
                           .FirstOrDefaultAsync(r => r.Id == reservation.Id && !r.IsDeleted, cancellationToken)
                       ?? throw new InvalidOperationException($"Facility reservation {reservation.Id} was not found.");

        await ValidateReservationAsync(reservation, reservation.Id, cancellationToken);
        if (reservation.Status == FacilityReservationStatus.Confirmed)
        {
            await EnsureNoConfirmedOverlapAsync(
                reservation.UnitId, reservation.StartUtc, reservation.EndUtc, reservation.Id, cancellationToken);
        }

        existing.FacilityRenterId = reservation.FacilityRenterId;
        existing.UnitId = reservation.UnitId;
        existing.StartUtc = EnsureUtc(reservation.StartUtc);
        existing.EndUtc = EnsureUtc(reservation.EndUtc);
        existing.Status = reservation.Status;
        existing.RentalFee = decimal.Round(reservation.RentalFee, 2, MidpointRounding.AwayFromZero);
        existing.DepositAmount = decimal.Round(reservation.DepositAmount, 2, MidpointRounding.AwayFromZero);
        existing.Notes = Trim(reservation.Notes, 2000);

        _db.Entry(existing).Property(e => e.RowVersion).OriginalValue = reservation.RowVersion;
        ConcurrencyHelper.BumpRowVersion(existing);
        await ConcurrencyHelper.SaveChangesOrThrowAsync(_db, "FacilityReservation", cancellationToken);

        if (existing.Status == FacilityReservationStatus.Confirmed)
        {
            await UpsertCalendarAsync(existing, cancellationToken);
        }
        else if (existing.ScheduledItemId is Guid sid)
        {
            await CancelCalendarAsync(sid, cancellationToken);
        }

        _logger.LogInformation(
            "Updated facility reservation {Id} status={Status} start={Start} end={End}.",
            existing.Id, existing.Status, existing.StartUtc, existing.EndUtc);
        return (await GetByIdAsync(existing.Id, cancellationToken))!;
    }

    public async Task<FacilityReservation> SetStatusAsync(
        Guid id,
        FacilityReservationStatus status,
        string? note = null,
        CancellationToken cancellationToken = default)
    {
        var existing = await _db.FacilityReservations
                           .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted, cancellationToken)
                       ?? throw new InvalidOperationException($"Facility reservation {id} was not found.");

        EnsureTransitionAllowed(existing.Status, status);

        if (status == FacilityReservationStatus.Confirmed)
        {
            await EnsureNoConfirmedOverlapAsync(
                existing.UnitId, existing.StartUtc, existing.EndUtc, existing.Id, cancellationToken);
        }

        if (status == FacilityReservationStatus.Completed)
        {
            var hasPost = await _db.FacilityInspections.AnyAsync(
                i => i.FacilityReservationId == id && i.Type == FacilityInspectionType.PostRental,
                cancellationToken);
            if (!hasPost && string.IsNullOrWhiteSpace(note))
            {
                throw new InvalidOperationException(
                    "Complete requires a PostRental inspection, or an override note.");
            }
        }

        existing.Status = status;
        if (!string.IsNullOrWhiteSpace(note))
        {
            existing.Notes = Trim(
                string.IsNullOrWhiteSpace(existing.Notes) ? note : $"{existing.Notes}\n{note}",
                2000);
        }

        ConcurrencyHelper.BumpRowVersion(existing);
        await ConcurrencyHelper.SaveChangesOrThrowAsync(_db, "FacilityReservation", cancellationToken);

        if (status == FacilityReservationStatus.Confirmed)
        {
            await UpsertCalendarAsync(existing, cancellationToken);
        }
        else if (status is FacilityReservationStatus.Cancelled or FacilityReservationStatus.Draft
                 && existing.ScheduledItemId is Guid sid)
        {
            await CancelCalendarAsync(sid, cancellationToken);
            existing.ScheduledItemId = null;
            await _db.SaveChangesAsync(cancellationToken);
        }

        _logger.LogInformation("Facility reservation {Id} status -> {Status}.", id, status);
        return (await GetByIdAsync(id, cancellationToken))!;
    }

    /// <summary>
    /// Allowed: Draft|Request → Confirmed|Cancelled; Confirmed → Cancelled|Completed;
    /// same-status is a no-op. Terminal Completed/Cancelled cannot leave.
    /// </summary>
    internal static void EnsureTransitionAllowed(
        FacilityReservationStatus from,
        FacilityReservationStatus to)
    {
        if (from == to)
        {
            return;
        }

        if (from is FacilityReservationStatus.Cancelled or FacilityReservationStatus.Completed)
        {
            throw new InvalidOperationException(
                $"Cannot change facility reservation status from {from} to {to}.");
        }

        var allowed = to switch
        {
            FacilityReservationStatus.Confirmed
                when from is FacilityReservationStatus.Draft or FacilityReservationStatus.Request
                => true,
            FacilityReservationStatus.Cancelled
                when from is FacilityReservationStatus.Draft or FacilityReservationStatus.Request
                    or FacilityReservationStatus.Confirmed
                => true,
            FacilityReservationStatus.Completed
                when from == FacilityReservationStatus.Confirmed
                => true,
            FacilityReservationStatus.Draft
                when from == FacilityReservationStatus.Request
                => true,
            FacilityReservationStatus.Request
                when from == FacilityReservationStatus.Draft
                => true,
            _ => false
        };

        if (!allowed)
        {
            throw new InvalidOperationException(
                $"Cannot change facility reservation status from {from} to {to}.");
        }
    }

    public async Task EnsureNoConfirmedOverlapAsync(
        Guid unitId,
        DateTime startUtc,
        DateTime endUtc,
        Guid? excludeId = null,
        CancellationToken cancellationToken = default)
    {
        startUtc = EnsureUtc(startUtc);
        endUtc = EnsureUtc(endUtc);
        var overlap = await _db.FacilityReservations.AsNoTracking()
            .AnyAsync(
                r => !r.IsDeleted
                     && r.UnitId == unitId
                     && r.Status == FacilityReservationStatus.Confirmed
                     && (excludeId == null || r.Id != excludeId)
                     && r.StartUtc < endUtc
                     && startUtc < r.EndUtc,
                cancellationToken);
        if (overlap)
        {
            _logger.LogWarning(
                "Confirmed facility reservation overlap rejected unit={UnitId} start={Start} end={End} exclude={ExcludeId}.",
                unitId, startUtc, endUtc, excludeId);
            throw new InvalidOperationException(
                "Confirmed Community Center reservation overlaps an existing confirmed booking.");
        }
    }

    private async Task UpsertCalendarAsync(FacilityReservation reservation, CancellationToken cancellationToken)
    {
        var renter = await _db.FacilityRenters.AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == reservation.FacilityRenterId, cancellationToken);
        var title = renter is null
            ? "CC rental"
            : $"CC rental — {renter.FirstName} {renter.LastName}";

        ScheduledItem item;
        if (reservation.ScheduledItemId is Guid sid)
        {
            item = await _db.ScheduledItems.FirstOrDefaultAsync(s => s.Id == sid, cancellationToken)
                   ?? new ScheduledItem { Id = Guid.NewGuid() };
            if (item.Id != sid)
            {
                _db.ScheduledItems.Add(item);
            }
        }
        else
        {
            item = new ScheduledItem { Id = Guid.NewGuid() };
            _db.ScheduledItems.Add(item);
        }

        item.Title = title;
        item.Category = ScheduledItemCategory.FacilityRental;
        item.UnitId = reservation.UnitId;
        item.FacilityReservationId = reservation.Id;
        item.StartUtc = reservation.StartUtc;
        item.EndUtc = reservation.EndUtc;
        item.DueUtc = reservation.StartUtc;
        item.IsCompleted = false;
        item.IsDeleted = false;
        item.Notes = reservation.Notes;

        reservation.ScheduledItemId = item.Id;
        var tracked = await _db.FacilityReservations.FirstAsync(r => r.Id == reservation.Id, cancellationToken);
        tracked.ScheduledItemId = item.Id;
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task CancelCalendarAsync(Guid scheduledItemId, CancellationToken cancellationToken)
    {
        var item = await _db.ScheduledItems.FirstOrDefaultAsync(s => s.Id == scheduledItemId, cancellationToken);
        if (item is null)
        {
            return;
        }

        item.IsDeleted = true;
        item.IsCompleted = true;
        item.CompletedUtc = _clock.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task ValidateReservationAsync(
        FacilityReservation reservation,
        Guid? excludeId,
        CancellationToken cancellationToken)
    {
        if (reservation.EndUtc <= reservation.StartUtc)
        {
            throw new ArgumentException("End must be after start.");
        }

        if (reservation.RentalFee < 0 || reservation.DepositAmount < 0)
        {
            throw new ArgumentException("Fee and deposit cannot be negative.");
        }

        var unit = await _db.Units.AsNoTracking()
                       .FirstOrDefaultAsync(u => u.Id == reservation.UnitId, cancellationToken)
                   ?? throw new InvalidOperationException($"Unit {reservation.UnitId} was not found.");
        if (!unit.IsFacility)
        {
            throw new InvalidOperationException("Reservations require the Community Center facility unit.");
        }

        if (!await _db.FacilityRenters.AnyAsync(
                r => r.Id == reservation.FacilityRenterId && !r.IsDeleted, cancellationToken))
        {
            throw new InvalidOperationException($"Facility renter {reservation.FacilityRenterId} was not found.");
        }

        reservation.StartUtc = EnsureUtc(reservation.StartUtc);
        reservation.EndUtc = EnsureUtc(reservation.EndUtc);
        _ = excludeId;
    }

    private IQueryable<FacilityReservation> BaseQuery() =>
        _db.FacilityReservations
            .AsNoTracking()
            .Include(r => r.FacilityRenter)
            .Include(r => r.Unit)
            .Where(r => !r.IsDeleted);

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
