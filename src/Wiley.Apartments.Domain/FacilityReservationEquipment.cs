namespace Wiley.Apartments.Domain;

/// <summary>Equipment requested for a Community Center reservation, from CC inventory.</summary>
public class FacilityReservationEquipment
{
    public Guid Id { get; set; }
    public Guid FacilityReservationId { get; set; }
    public Guid InventoryItemId { get; set; }
    public FacilityInventoryItem? InventoryItem { get; set; }
    public int Quantity { get; set; } = 1;
}
