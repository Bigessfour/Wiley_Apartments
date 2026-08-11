namespace Wiley.Apartments.Domain;

public class MaintenanceRequest
{
    public Guid Id { get; set; }
    public Guid UnitId { get; set; }
    public Unit? Unit { get; set; }
    public Guid? AssetId { get; set; }
    public Asset? Asset { get; set; }
    public string Description { get; set; } = string.Empty;
    public MaintenanceStatus Status { get; set; } = MaintenanceStatus.Open;
    public MaintenancePriority Priority { get; set; } = MaintenancePriority.Normal;
    public decimal? Cost { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime? CompletedUtc { get; set; }
    public string? CompletedByUserId { get; set; }
    public string? CompletedByDisplay { get; set; }
    public Guid? FacilityReservationId { get; set; }
    public FacilityReservation? FacilityReservation { get; set; }
    public string? Notes { get; set; }
    public bool IsDeleted { get; set; }
    /// <summary>When a completed WO posts landlord expense, links to <see cref="UnitOperatingCost"/>.</summary>
    public Guid? OperatingCostId { get; set; }
}
