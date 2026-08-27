using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Wiley.Apartments.Domain;
using Wiley.Apartments.Web.Data;
using Wiley.Apartments.Web.Services;

namespace Wiley.Apartments.Tests.Services;

public class FacilityRenterServiceTests
{
    private static (ApartmentsDbContext Db, FacilityRenterService Service) Create()
    {
        var connection = new Microsoft.Data.Sqlite.SqliteConnection("Data Source=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<ApartmentsDbContext>()
            .UseSqlite(connection)
            .Options;
        var db = new ApartmentsDbContext(options);
        db.Database.EnsureCreated();
        return (db, new FacilityRenterService(db, NullLogger<FacilityRenterService>.Instance));
    }

    private static FacilityRenter Sample(string phone) => new()
    {
        FirstName = "Pat",
        LastName = "Nguyen",
        Phone = phone,
        Email = "pat@example.com",
        MailingAddress = "100 Main St Wiley CO"
    };

    [Fact]
    public void FormatUsPhone_FormatsTenDigits()
    {
        FacilityRenterService.FormatUsPhone("7195550100").Should().Be("(719) 555-0100");
        FacilityRenterService.FormatUsPhone("719-555-0100").Should().Be("(719) 555-0100");
        FacilityRenterService.FormatUsPhone("1 (719) 555-0100").Should().Be("(719) 555-0100");
        FacilityRenterService.FormatUsPhone("(719) 555-0100").Should().Be("(719) 555-0100");
    }

    [Fact]
    public void FormatUsPhone_IncompleteMask_IsEmpty()
    {
        FacilityRenterService.FormatUsPhone("(___) ___-____").Should().BeEmpty();
    }

    [Fact]
    public async Task CreateAsync_PersistsFormattedPhoneAndIdFields()
    {
        var (db, service) = Create();
        await using (db)
        {
            var renter = Sample("7195550199");
            renter.IdType = "Driver's license";
            renter.IdReference = "4321";
            var created = await service.CreateAsync(renter);

            created.Phone.Should().Be("(719) 555-0199");
            created.IdType.Should().Be("Driver's license");
            created.IdReference.Should().Be("4321");
        }
    }
}
