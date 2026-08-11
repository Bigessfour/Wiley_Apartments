using Microsoft.EntityFrameworkCore;
using Wiley.Apartments.Contracts;
using Wiley.Apartments.Domain;
using Wiley.Apartments.Web.Data;

namespace Wiley.Apartments.Web.Services;

public sealed class FacilityRentalAgreementService(
    ApartmentsDbContext db,
    IDocumentService documents,
    IDateTimeService clock,
    FacilityRentalAgreementGenerator generator,
    ILogger<FacilityRentalAgreementService> logger) : IFacilityRentalAgreementService
{
    private readonly ApartmentsDbContext _db = db;
    private readonly IDocumentService _documents = documents;
    private readonly IDateTimeService _clock = clock;
    private readonly FacilityRentalAgreementGenerator _generator = generator;
    private readonly ILogger<FacilityRentalAgreementService> _logger = logger;

    public async Task<FacilityAgreementResult> GenerateAsync(
        Guid reservationId,
        CancellationToken cancellationToken = default)
    {
        var reservation = await _db.FacilityReservations
                              .Include(r => r.FacilityRenter)
                              .FirstOrDefaultAsync(r => r.Id == reservationId && !r.IsDeleted, cancellationToken)
                          ?? throw new InvalidOperationException($"Facility reservation {reservationId} was not found.");

        var renter = reservation.FacilityRenter
                     ?? throw new InvalidOperationException("Reservation is missing renter data.");

        var missing = new List<string>();
        if (string.IsNullOrWhiteSpace(renter.FirstName) || string.IsNullOrWhiteSpace(renter.LastName))
        {
            missing.Add("renter name");
        }

        if (string.IsNullOrWhiteSpace(renter.MailingAddress))
        {
            missing.Add("mailing address");
        }

        if (string.IsNullOrWhiteSpace(renter.Phone))
        {
            missing.Add("phone");
        }

        if (string.IsNullOrWhiteSpace(renter.Email))
        {
            missing.Add("email");
        }

        if (missing.Count > 0)
        {
            throw new InvalidOperationException(
                "Cannot generate agreement; missing: " + string.Join(", ", missing));
        }

        var startLocal = _clock.ToDisplayTime(reservation.StartUtc);
        var endLocal = _clock.ToDisplayTime(reservation.EndUtc);
        var generatedLocal = _clock.ToDisplayTime(_clock.UtcNow);

        var pdf = _generator.Generate(new FacilityRentalAgreementData(
            $"{renter.FirstName} {renter.LastName}".Trim(),
            renter.Organization ?? "—",
            renter.MailingAddress,
            renter.Phone,
            renter.Email,
            startLocal.ToString("yyyy-MM-dd HH:mm"),
            endLocal.ToString("yyyy-MM-dd HH:mm"),
            reservation.RentalFee.ToString("C"),
            reservation.DepositAmount.ToString("C"),
            reservation.Notes,
            generatedLocal.ToString("yyyy-MM-dd HH:mm")));

        var relativeDir = $"community-center/reservations/{reservation.Id:N}";
        var fileName = $"agreement-generated-{generatedLocal:yyyyMMddHHmm}.pdf";
        await using var stream = new MemoryStream(pdf);
        var doc = await _documents.UploadAsync(
            DocumentEntityType.FacilityReservation,
            reservation.Id,
            DocumentCategory.FacilityAgreement,
            fileName,
            "application/pdf",
            stream,
            "clerk",
            relativeDir,
            cancellationToken);

        var relPath = doc.FilePathOnNas;
        reservation.GeneratedPdfRelativePath = relPath;
        ConcurrencyHelper.BumpRowVersion(reservation);
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Generated CC rental agreement for reservation {Id} -> {Path}",
            reservation.Id, relPath);

        return new FacilityAgreementResult(reservation.Id, pdf, relPath, fileName, doc.Id);
    }

    public async Task AttachSignedAsync(
        Guid reservationId,
        Stream pdf,
        string fileName,
        string uploadedBy,
        CancellationToken cancellationToken = default)
    {
        var reservation = await _db.FacilityReservations
                              .FirstOrDefaultAsync(r => r.Id == reservationId && !r.IsDeleted, cancellationToken)
                          ?? throw new InvalidOperationException($"Facility reservation {reservationId} was not found.");

        var relativeDir = $"community-center/reservations/{reservation.Id:N}";
        var doc = await _documents.UploadAsync(
            DocumentEntityType.FacilityReservation,
            reservation.Id,
            DocumentCategory.FacilityAgreement,
            string.IsNullOrWhiteSpace(fileName) ? "agreement-signed.pdf" : fileName,
            "application/pdf",
            pdf,
            string.IsNullOrWhiteSpace(uploadedBy) ? "clerk" : uploadedBy,
            relativeDir,
            cancellationToken);

        reservation.SignedDocumentId = doc.Id;
        ConcurrencyHelper.BumpRowVersion(reservation);
        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Attached signed CC agreement {DocId} to reservation {Id}.", doc.Id, reservationId);
    }
}
