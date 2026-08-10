namespace Wiley.Apartments.Domain;

public class Tenant
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string EmergencyContact { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public bool IsDeleted { get; set; }
    /// <summary>Optimistic concurrency token (SQLite-friendly Guid).</summary>
    public Guid RowVersion { get; set; } = Guid.NewGuid();

    public List<HouseholdMember> HouseholdMembers { get; set; } = [];
    public List<Vehicle> Vehicles { get; set; } = [];
    public List<Pet> Pets { get; set; } = [];
}
