namespace Wiley.Apartments.Contracts;

public interface IFacilityRentalAgreementService
{
    Task<FacilityAgreementResult> GenerateAsync(
        Guid reservationId,
        CancellationToken cancellationToken = default);

    Task AttachSignedAsync(
        Guid reservationId,
        Stream pdf,
        string fileName,
        string uploadedBy,
        CancellationToken cancellationToken = default);
}

public sealed record FacilityAgreementResult(
    Guid ReservationId,
    byte[] PdfBytes,
    string RelativePath,
    string FileName,
    Guid DocumentId);
