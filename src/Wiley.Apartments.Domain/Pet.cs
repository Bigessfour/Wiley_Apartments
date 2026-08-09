namespace Wiley.Apartments.Domain;

public class Pet
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? Breed { get; set; }
    public string? Notes { get; set; }
}
