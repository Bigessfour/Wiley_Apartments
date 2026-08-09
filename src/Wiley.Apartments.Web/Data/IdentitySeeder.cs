using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Wiley.Apartments.Web.Data;

public sealed class IdentitySeeder : IIdentitySeeder
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SeedUserOptions _seedOptions;
    private readonly ILogger<IdentitySeeder> _logger;

    public IdentitySeeder(
        UserManager<ApplicationUser> userManager,
        IOptions<SeedUserOptions> seedOptions,
        ILogger<IdentitySeeder> logger)
    {
        _userManager = userManager;
        _seedOptions = seedOptions.Value;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        foreach (var seedUser in _seedOptions.Users)
        {
            if (string.IsNullOrWhiteSpace(seedUser.Email) || string.IsNullOrWhiteSpace(seedUser.Password))
            {
                continue;
            }

            var existing = await _userManager.FindByEmailAsync(seedUser.Email);
            if (existing is not null)
            {
                continue;
            }

            var user = new ApplicationUser
            {
                UserName = seedUser.Email,
                Email = seedUser.Email,
                EmailConfirmed = true,
                DisplayName = seedUser.DisplayName ?? seedUser.Email
            };

            var result = await _userManager.CreateAsync(user, seedUser.Password);
            if (result.Succeeded)
            {
                _logger.LogInformation("Seeded Identity user {Email}.", seedUser.Email);
            }
            else
            {
                _logger.LogWarning(
                    "Failed to seed user {Email}: {Errors}",
                    seedUser.Email,
                    string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
    }
}
