namespace Wiley.Apartments.Domain;

/// <summary>Community Center hall booking for a date range.</summary>
public class FacilityReservation
{
    public Guid Id { get; set; }
    public Guid UnitId { get; set; }
    public Unit? Unit { get; set; }
    public Guid FacilityRenterId { get; set; }
    public FacilityRenter? FacilityRenter { get; set; }
    public DateTime StartUtc { get; set; }
    public DateTime EndUtc { get; set; }
    public FacilityReservationStatus Status { get; set; } = FacilityReservationStatus.Draft;
    public decimal RentalFee { get; set; }
    public decimal DepositAmount { get; set; }
    public string? Notes { get; set; }
    public string? GeneratedPdfRelativePath { get; set; }
    public Guid? SignedDocumentId { get; set; }
    public Guid? ScheduledItemId { get; set; }
    public bool IsDeleted { get; set; }
    public Guid RowVersion { get; set; } = Guid.NewGuid();
}
