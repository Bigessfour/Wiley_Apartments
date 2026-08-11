using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Wiley.Apartments.Domain;
using Wiley.Apartments.Tests.Support;
using Wiley.Apartments.Web.Configuration;
using Wiley.Apartments.Web.Services;

namespace Wiley.Apartments.Tests.Services;

public sealed class UnitConcurrencyTests
{
    [Fact]
    public async Task UpdateAsync_StaleRowVersion_ThrowsConcurrencyConflict()
    {
        using var dbFactory = new SqliteTestDatabase();
        await using var db = dbFactory.CreateContext();
        var unit = new Unit
        {
            Id = Guid.NewGuid(),
            Number = "1",
            SqFt = 800,
            Beds = 2,
            Baths = 1,
            Status = UnitStatus.Vacant,
            RowVersion = Guid.NewGuid()
        };
        db.Units.Add(unit);
        await db.SaveChangesAsync();

        var options = Options.Create(new ClerkSuiteOptions { MaxUnits = 16 });
        var service = new UnitService(db, options, NullLogger<UnitService>.Instance);

        var stale = new Unit
        {
            Id = unit.Id,
            Number = "1",
            SqFt = 900,
            Beds = 2,
            Baths = 1,
            Status = UnitStatus.Vacant,
            RowVersion = Guid.NewGuid()
        };

        var act = async () => await service.UpdateAsync(stale);
        await act.Should().ThrowAsync<ConcurrencyConflictException>()
            .WithMessage("*another clerk*");
    }
}
