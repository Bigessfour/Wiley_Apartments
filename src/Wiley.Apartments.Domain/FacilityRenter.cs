namespace Wiley.Apartments.Domain;

/// <summary>Community Center event/hall hirer — not a residential <see cref="Tenant"/>.</summary>
public class FacilityRenter
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Organization { get; set; }
    public string MailingAddress { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? AlternateContact { get; set; }
    public string? IdType { get; set; }
    public string? IdReference { get; set; }
    public string? Notes { get; set; }
    public bool IsDeleted { get; set; }
    public Guid RowVersion { get; set; } = Guid.NewGuid();
}
