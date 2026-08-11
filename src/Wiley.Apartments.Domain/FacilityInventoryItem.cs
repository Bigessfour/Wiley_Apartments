namespace Wiley.Apartments.Domain;

/// <summary>Countable / tracked equipment belonging to the Community Center facility unit.</summary>
public class FacilityInventoryItem
{
    public Guid Id { get; set; }
    public Guid UnitId { get; set; }
    public Unit? Unit { get; set; }
    public FacilityInventoryCategory Category { get; set; } = FacilityInventoryCategory.Other;
    public string Name { get; set; } = string.Empty;
    public int Quantity { get; set; } = 1;
    public string Condition { get; set; } = "Good";
    public string? Location { get; set; }
    public string? Serial { get; set; }
    public string? Notes { get; set; }
    public bool IsDeleted { get; set; }
    public Guid RowVersion { get; set; } = Guid.NewGuid();
}
