namespace Wiley.Apartments.Web.Data;

public class SeedUserOptions
{
    public const string SectionName = "SeedUsers";
    public List<SeedUser> Users { get; set; } = [];
}

public class SeedUser
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
}
