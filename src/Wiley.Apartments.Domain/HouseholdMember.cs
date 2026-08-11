namespace Wiley.Apartments.Domain;

public class HouseholdMember
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Relationship { get; set; } = string.Empty;
    public DateOnly? DateOfBirth { get; set; }
}
