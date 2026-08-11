namespace Wiley.Apartments.Domain;

public class FacilityInspection
{
    public Guid Id { get; set; }
    public Guid FacilityReservationId { get; set; }
    public FacilityReservation? FacilityReservation { get; set; }
    public FacilityInspectionType Type { get; set; } = FacilityInspectionType.PostRental;
    public bool IsSatisfactory { get; set; }
    public string? ChecklistNotes { get; set; }
    public string? DamageNotes { get; set; }
    public string? InspectorUserId { get; set; }
    public string InspectorDisplay { get; set; } = string.Empty;
    public DateTime InspectedUtc { get; set; }
    public Guid RowVersion { get; set; } = Guid.NewGuid();
}
