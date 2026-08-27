namespace Wiley.Apartments.Domain;

/// <summary>Default CC rental fee/deposit for a room or whole-building package. Clerks may override per booking.</summary>
public class FacilityRentalRate
{
    public Guid Id { get; set; }
    public FacilitySpace Space { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Fee { get; set; }
    public decimal Deposit { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}
