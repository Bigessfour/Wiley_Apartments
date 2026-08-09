using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Wiley.Apartments.Contracts;
using Wiley.Apartments.Domain;
using Wiley.Apartments.Web.Configuration;
using Wiley.Apartments.Web.Data;

namespace Wiley.Apartments.Web.Data;

public sealed class UnitSeeder : IUnitSeeder
{
    private readonly ApartmentsDbContext _db;
    private readonly ClerkSuiteOptions _options;
    private readonly ILogger<UnitSeeder> _logger;

    public UnitSeeder(
        ApartmentsDbContext db,
        IOptions<ClerkSuiteOptions> options,
        ILogger<UnitSeeder> logger)
    {
        _db = db;
        _options = options.Value;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (await _db.Units.AnyAsync(cancellationToken))
        {
            return;
        }

        var max = Math.Min(_options.MaxUnits, 16);
        for (var i = 1; i <= max; i++)
        {
            _db.Units.Add(new Unit
            {
                Id = Guid.NewGuid(),
                Number = i.ToString(),
                SqFt = 0,
                Beds = 0,
                Baths = 0,
                Status = UnitStatus.Vacant,
                Notes = "Placeholder — update with real unit data (G1)."
            });
        }

        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Seeded {Count} placeholder units.", max);
    }
}
