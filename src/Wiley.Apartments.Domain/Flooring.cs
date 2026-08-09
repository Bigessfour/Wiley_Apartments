namespace Wiley.Apartments.Domain;

public class Flooring
{
    public Guid Id { get; set; }
    public Guid UnitId { get; set; }
    public Unit? Unit { get; set; }
    public string Type { get; set; } = string.Empty;
    public DateOnly? InstallDate { get; set; }
    public string Condition { get; set; } = string.Empty;
    public DateOnly? ReplacedDate { get; set; }
    public string? Notes { get; set; }
}
