using Microsoft.EntityFrameworkCore;
using Wiley.Apartments.Contracts;
using Wiley.Apartments.Domain;
using Wiley.Apartments.Web.Data;

namespace Wiley.Apartments.Web.Services;

public sealed class FacilityRenterService(
    ApartmentsDbContext db,
    ILogger<FacilityRenterService> logger) : IFacilityRenterService
{
    private readonly ApartmentsDbContext _db = db;
    private readonly ILogger<FacilityRenterService> _logger = logger;

    public async Task<IReadOnlyList<FacilityRenter>> SearchAsync(
        string? query = null,
        bool includeDeleted = false,
        CancellationToken cancellationToken = default)
    {
        var q = _db.FacilityRenters.AsNoTracking().AsQueryable();
        if (!includeDeleted)
        {
            q = q.Where(r => !r.IsDeleted);
        }

        if (!string.IsNullOrWhiteSpace(query))
        {
            var term = query.Trim().ToLowerInvariant();
            q = q.Where(r =>
                r.LastName.ToLower().Contains(term)
                || r.FirstName.ToLower().Contains(term)
                || (r.Organization != null && r.Organization.ToLower().Contains(term))
                || r.Email.ToLower().Contains(term)
                || r.Phone.ToLower().Contains(term));
        }

        return await q
            .OrderBy(r => r.LastName)
            .ThenBy(r => r.FirstName)
            .ToListAsync(cancellationToken);
    }

    public async Task<FacilityRenter?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _db.FacilityRenters.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public async Task<FacilityRenter> CreateAsync(FacilityRenter renter, CancellationToken cancellationToken = default)
    {
        Validate(renter);
        renter.Id = Guid.NewGuid();
        renter.IsDeleted = false;
        renter.RowVersion = Guid.NewGuid();
        _db.FacilityRenters.Add(renter);
        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Created facility renter {Id} {LastName}, {FirstName}.", renter.Id, renter.LastName, renter.FirstName);
        return renter;
    }

    public async Task<FacilityRenter> UpdateAsync(FacilityRenter renter, CancellationToken cancellationToken = default)
    {
        Validate(renter);
        var existing = await _db.FacilityRenters.FindAsync([renter.Id], cancellationToken)
            ?? throw new InvalidOperationException($"Facility renter {renter.Id} was not found.");
        if (existing.IsDeleted)
        {
            throw new InvalidOperationException("Cannot update a soft-deleted facility renter.");
        }

        existing.FirstName = renter.FirstName.Trim();
        existing.LastName = renter.LastName.Trim();
        existing.Organization = TrimOrNull(renter.Organization, 256);
        existing.MailingAddress = renter.MailingAddress.Trim();
        existing.Phone = renter.Phone.Trim();
        existing.Email = renter.Email.Trim();
        existing.AlternateContact = TrimOrNull(renter.AlternateContact, 512);
        existing.IdType = TrimOrNull(renter.IdType, 64);
        existing.IdReference = TrimOrNull(renter.IdReference, 64);
        existing.Notes = TrimOrNull(renter.Notes, 2000);

        _db.Entry(existing).Property(e => e.RowVersion).OriginalValue = renter.RowVersion;
        ConcurrencyHelper.BumpRowVersion(existing);
        await ConcurrencyHelper.SaveChangesOrThrowAsync(_db, "FacilityRenter", cancellationToken);
        _logger.LogInformation("Updated facility renter {Id}.", existing.Id);
        return existing;
    }

    public async Task SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var renter = await _db.FacilityRenters.FindAsync([id], cancellationToken)
            ?? throw new InvalidOperationException($"Facility renter {id} was not found.");
        if (renter.IsDeleted)
        {
            return;
        }

        var now = DateTime.UtcNow;
        var hasFuture = await _db.FacilityReservations.AnyAsync(
            r => r.FacilityRenterId == id
                 && !r.IsDeleted
                 && (r.Status == FacilityReservationStatus.Request || r.Status == FacilityReservationStatus.Confirmed)
                 && r.EndUtc >= now,
            cancellationToken);
        if (hasFuture)
        {
            throw new InvalidOperationException(
                "Cannot delete a facility renter with future Request/Confirmed reservations. Cancel those first.");
        }

        renter.IsDeleted = true;
        ConcurrencyHelper.BumpRowVersion(renter);
        await ConcurrencyHelper.SaveChangesOrThrowAsync(_db, "FacilityRenter", cancellationToken);
        _logger.LogInformation("Soft-deleted facility renter {Id}.", id);
    }

    private static void Validate(FacilityRenter renter)
    {
        if (string.IsNullOrWhiteSpace(renter.FirstName) || string.IsNullOrWhiteSpace(renter.LastName))
        {
            throw new ArgumentException("First and last name are required.");
        }

        if (string.IsNullOrWhiteSpace(renter.Phone) || string.IsNullOrWhiteSpace(renter.Email))
        {
            throw new ArgumentException("Phone and email are required.");
        }

        if (string.IsNullOrWhiteSpace(renter.MailingAddress))
        {
            throw new ArgumentException("Mailing address is required.");
        }

        renter.FirstName = renter.FirstName.Trim();
        renter.LastName = renter.LastName.Trim();
        renter.Phone = FormatUsPhone(renter.Phone);
        renter.Email = renter.Email.Trim();
        renter.MailingAddress = renter.MailingAddress.Trim();
    }

    /// <summary>Formats a 10-digit US number as (719) 555-0100; otherwise returns trimmed input.</summary>
    internal static string FormatUsPhone(string phone)
    {
        var trimmed = phone.Trim();
        var digits = new string(trimmed.Where(char.IsDigit).ToArray());
        if (digits.Length == 0)
        {
            return string.Empty;
        }

        if (digits.Length == 11 && digits[0] == '1')
        {
            digits = digits[1..];
        }

        if (digits.Length == 10)
        {
            return $"({digits[..3]}) {digits[3..6]}-{digits[6..]}";
        }

        return trimmed;
    }

    private static string? TrimOrNull(string? value, int max)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var t = value.Trim();
        return t.Length > max ? t[..max] : t;
    }
}
