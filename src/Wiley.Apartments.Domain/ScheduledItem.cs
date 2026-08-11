namespace Wiley.Apartments.Domain;

/// <summary>Clerk calendar item for unit-linked date work (Phase 3.5 Scheduler).</summary>
public class ScheduledItem
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public ScheduledItemCategory Category { get; set; } = ScheduledItemCategory.Other;
    public Guid? UnitId { get; set; }
    public Unit? Unit { get; set; }
    public Guid? TenantId { get; set; }
    public Tenant? Tenant { get; set; }
    public Guid? LeaseId { get; set; }
    public Lease? Lease { get; set; }
    public Guid? FacilityReservationId { get; set; }
    public FacilityReservation? FacilityReservation { get; set; }
    public DateTime StartUtc { get; set; }
    public DateTime? EndUtc { get; set; }
    public DateTime? DueUtc { get; set; }
    /// <summary>How long before <see cref="DueUtc"/> (or <see cref="StartUtc"/>) to surface a reminder.</summary>
    public TimeSpan? ReminderOffset { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime? CompletedUtc { get; set; }
    public string? Notes { get; set; }
    public bool IsDeleted { get; set; }
}
