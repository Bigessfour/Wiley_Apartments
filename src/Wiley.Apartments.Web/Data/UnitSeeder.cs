using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Wiley.Apartments.Contracts;
using Wiley.Apartments.Domain;
using Wiley.Apartments.Web.Configuration;
using Wiley.Apartments.Web.Data;
using Wiley.Apartments.Web.Services;

namespace Wiley.Apartments.Web.Data;

public sealed class UnitSeeder(
    ApartmentsDbContext db,
    IOptions<ClerkSuiteOptions> options,
    ILogger<UnitSeeder> logger) : IUnitSeeder
{
    private readonly ApartmentsDbContext _db = db;
    private readonly ClerkSuiteOptions _options = options.Value;
    private readonly ILogger<UnitSeeder> _logger = logger;

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var hasResidential = await _db.Units.AnyAsync(u => !u.IsFacility, cancellationToken);
        if (!hasResidential)
        {
            // Initial portfolio seed stays at 16 placeholders; MaxUnits=0 means unlimited creates after that.
            var max = _options.MaxUnits > 0 ? Math.Min(_options.MaxUnits, 16) : 16;
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
                    IsFacility = false,
                    Notes = "Placeholder — update with real unit data (G1)."
                });
            }

            await _db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Seeded {Count} placeholder residential units.", max);
        }

        await EnsureCommunityCenterAsync(cancellationToken);
    }

    private async Task EnsureCommunityCenterAsync(CancellationToken cancellationToken)
    {
        var exists = await _db.Units.AnyAsync(
            u => u.IsFacility || u.Number == UnitService.CommunityCenterNumber,
            cancellationToken);
        if (exists)
        {
            // Repair: ensure flag if Number=CC was seeded without IsFacility.
            var cc = await _db.Units.FirstOrDefaultAsync(
                u => u.Number == UnitService.CommunityCenterNumber && !u.IsFacility,
                cancellationToken);
            if (cc is not null)
            {
                cc.IsFacility = true;
                await _db.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Marked unit CC as facility.");
            }

            return;
        }

        _db.Units.Add(new Unit
        {
            Id = Guid.NewGuid(),
            Number = UnitService.CommunityCenterNumber,
            SqFt = 0,
            Beds = 0,
            Baths = 0,
            Status = UnitStatus.Vacant,
            IsFacility = true,
            Notes = "Community Center — facility rental / events. Uses existing Schedule, Ledger, and Maintenance tools."
        });
        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Seeded Community Center facility unit (Number=CC).");
    }
}
