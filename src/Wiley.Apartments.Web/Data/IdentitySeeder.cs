using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Wiley.Apartments.Web.Data;

public sealed class IdentitySeeder : IIdentitySeeder
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SeedUserOptions _seedOptions;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<IdentitySeeder> _logger;

    public IdentitySeeder(
        UserManager<ApplicationUser> userManager,
        IOptions<SeedUserOptions> seedOptions,
        IHostEnvironment environment,
        ILogger<IdentitySeeder> logger)
    {
        _userManager = userManager;
        _seedOptions = seedOptions.Value;
        _environment = environment;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await SeedConfiguredUsersAsync(cancellationToken);
        await SeedDevelopmentUserAsync(cancellationToken);
    }

    private async Task SeedConfiguredUsersAsync(CancellationToken cancellationToken)
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

    private async Task SeedDevelopmentUserAsync(CancellationToken cancellationToken)
    {
        if (!_environment.IsDevelopment())
        {
            return;
        }

        const string devEmail = "clerk@dev.local";
        if (await _userManager.FindByEmailAsync(devEmail) is not null)
        {
            return;
        }

        var user = new ApplicationUser
        {
            UserName = devEmail,
            Email = devEmail,
            EmailConfirmed = true,
            DisplayName = "Dev Clerk"
        };

        var result = await _userManager.CreateAsync(user, "Password1!");
        if (result.Succeeded)
        {
            _logger.LogInformation("Seeded development user {Email}.", devEmail);
        }
        else
        {
            _logger.LogWarning(
                "Failed to seed development user {Email}: {Errors}",
                devEmail,
                string.Join(", ", result.Errors.Select(e => e.Description)));
        }
    }
}
