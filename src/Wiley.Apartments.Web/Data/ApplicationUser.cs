using Microsoft.AspNetCore.Identity;

namespace Wiley.Apartments.Web.Data;

public class ApplicationUser : IdentityUser
{
    public string? DisplayName { get; set; }
}
