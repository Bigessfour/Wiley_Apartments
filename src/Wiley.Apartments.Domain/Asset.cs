namespace Wiley.Apartments.Domain;

public class Asset
{
    public Guid Id { get; set; }
    public Guid UnitId { get; set; }
    public Unit? Unit { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Make { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string Serial { get; set; } = string.Empty;
    public DateOnly? InstallDate { get; set; }
    public DateOnly? WarrantyStart { get; set; }
    public DateOnly? WarrantyEnd { get; set; }
    public string Condition { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public string? PhotoPaths { get; set; }
}
