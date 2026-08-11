using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Wiley.Apartments.Domain;
using Wiley.Apartments.Web.Data;
using Wiley.Apartments.Web.Services;

namespace Wiley.Apartments.Tests.Services;

public class TenantServiceTests
{
    private static TenantService CreateService(ApartmentsDbContext db) =>
        new(db, NullLogger<TenantService>.Instance);

    private static ApartmentsDbContext CreateContext()
    {
        var connection = new Microsoft.Data.Sqlite.SqliteConnection("Data Source=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<ApartmentsDbContext>()
            .UseSqlite(connection)
            .Options;
        var db = new ApartmentsDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }

    [Fact]
    public async Task CreateAsync_RequiresNames()
    {
        await using var db = CreateContext();
        var service = CreateService(db);

        var act = () => service.CreateAsync(new Tenant { FirstName = " ", LastName = "Smith" });

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*First name*");
    }

    [Fact]
    public async Task SearchAsync_ExcludesSoftDeleted_ByDefault()
    {
        await using var db = CreateContext();
        var service = CreateService(db);
        var active = await service.CreateAsync(new Tenant { FirstName = "Ann", LastName = "Active" });
        var gone = await service.CreateAsync(new Tenant { FirstName = "Bob", LastName = "Gone" });
        await service.SoftDeleteAsync(gone.Id);

        var results = await service.SearchAsync();

        results.Should().ContainSingle(t => t.Id == active.Id);
        results.Should().NotContain(t => t.Id == gone.Id);
    }

    [Fact]
    public async Task SearchAsync_FindsByLastName()
    {
        await using var db = CreateContext();
        var service = CreateService(db);
        await service.CreateAsync(new Tenant { FirstName = "Pat", LastName = "Nguyen" });
        await service.CreateAsync(new Tenant { FirstName = "Sam", LastName = "Lee" });

        var results = await service.SearchAsync("nguy");

        results.Should().ContainSingle();
        results[0].LastName.Should().Be("Nguyen");
    }

    [Fact]
    public async Task SearchAsync_CurrentOnly_IncludesOpenOccupancyAndProspects_ExcludesFormer()
    {
        await using var db = CreateContext();
        var service = CreateService(db);

        var current = await service.CreateAsync(new Tenant { FirstName = "Cur", LastName = "Rent" });
        var prospect = await service.CreateAsync(new Tenant { FirstName = "New", LastName = "Prospect" });
        var former = await service.CreateAsync(new Tenant { FirstName = "For", LastName = "Mer" });

        var unitCurrent = new Unit { Id = Guid.NewGuid(), Number = "1", SqFt = 700, Beds = 2, Baths = 1 };
        var unitFormer = new Unit { Id = Guid.NewGuid(), Number = "2", SqFt = 700, Beds = 2, Baths = 1 };
        db.Units.AddRange(unitCurrent, unitFormer);
        db.Occupancies.AddRange(
            new Occupancy
            {
                Id = Guid.NewGuid(),
                UnitId = unitCurrent.Id,
                TenantId = current.Id,
                StartUtc = DateTime.UtcNow.AddMonths(-2),
                EndUtc = null
            },
            new Occupancy
            {
                Id = Guid.NewGuid(),
                UnitId = unitFormer.Id,
                TenantId = former.Id,
                StartUtc = DateTime.UtcNow.AddYears(-1),
                EndUtc = DateTime.UtcNow.AddMonths(-1)
            });
        await db.SaveChangesAsync();

        var currentOnly = await service.SearchAsync(currentOnly: true);

        currentOnly.Select(t => t.Id).Should().BeEquivalentTo([current.Id, prospect.Id]);
        currentOnly.Should().NotContain(t => t.Id == former.Id);
    }

    [Fact]
    public async Task SearchAsync_IncludeFormer_ReturnsEndedOccupancyTenants()
    {
        await using var db = CreateContext();
        var service = CreateService(db);

        var former = await service.CreateAsync(new Tenant { FirstName = "For", LastName = "Mer" });
        var unit = new Unit { Id = Guid.NewGuid(), Number = "2", SqFt = 700, Beds = 2, Baths = 1 };
        db.Units.Add(unit);
        db.Occupancies.Add(new Occupancy
        {
            Id = Guid.NewGuid(),
            UnitId = unit.Id,
            TenantId = former.Id,
            StartUtc = DateTime.UtcNow.AddYears(-1),
            EndUtc = DateTime.UtcNow.AddMonths(-1)
        });
        await db.SaveChangesAsync();

        var all = await service.SearchAsync(currentOnly: false);

        all.Should().Contain(t => t.Id == former.Id);
    }

    [Fact]
    public async Task SearchAsync_CurrentOnly_StillExcludesSoftDeleted()
    {
        await using var db = CreateContext();
        var service = CreateService(db);

        var prospect = await service.CreateAsync(new Tenant { FirstName = "Ann", LastName = "Active" });
        var deleted = await service.CreateAsync(new Tenant { FirstName = "Bob", LastName = "Gone" });
        await service.SoftDeleteAsync(deleted.Id);

        var results = await service.SearchAsync(currentOnly: true);

        results.Should().ContainSingle(t => t.Id == prospect.Id);
        results.Should().NotContain(t => t.Id == deleted.Id);
    }

    [Fact]
    public async Task SoftDeleteAsync_BlocksUpdate()
    {
        await using var db = CreateContext();
        var service = CreateService(db);
        var tenant = await service.CreateAsync(new Tenant { FirstName = "Kim", LastName = "Old" });
        await service.SoftDeleteAsync(tenant.Id);
        tenant.FirstName = "Updated";

        var act = () => service.UpdateAsync(tenant);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*soft-deleted*");
    }

    [Fact]
    public async Task AddHouseholdMemberAsync_Persists()
    {
        await using var db = CreateContext();
        var service = CreateService(db);
        var tenant = await service.CreateAsync(new Tenant { FirstName = "Jo", LastName = "Parent" });

        await service.AddHouseholdMemberAsync(tenant.Id, new HouseholdMember
        {
            FullName = "Kid Parent",
            Relationship = "Child"
        });

        var loaded = await service.GetByIdAsync(tenant.Id);
        loaded!.HouseholdMembers.Should().ContainSingle(m => m.FullName == "Kid Parent");
    }

    [Fact]
    public async Task UpdateHouseholdMemberAsync_PersistsChanges()
    {
        await using var db = CreateContext();
        var service = CreateService(db);
        var tenant = await service.CreateAsync(new Tenant { FirstName = "Jo", LastName = "Parent" });
        var member = await service.AddHouseholdMemberAsync(tenant.Id, new HouseholdMember
        {
            FullName = "Kid Parent",
            Relationship = "Child"
        });

        member.FullName = "Kid Updated";
        member.Relationship = "Dependent";
        await service.UpdateHouseholdMemberAsync(member);

        var loaded = await service.GetByIdAsync(tenant.Id);
        loaded!.HouseholdMembers.Should().ContainSingle(m => m.FullName == "Kid Updated" && m.Relationship == "Dependent");
    }

    [Fact]
    public async Task UpdateVehicleAsync_PersistsPlate()
    {
        await using var db = CreateContext();
        var service = CreateService(db);
        var tenant = await service.CreateAsync(new Tenant { FirstName = "Al", LastName = "Driver" });
        var vehicle = await service.AddVehicleAsync(tenant.Id, new Vehicle
        {
            Make = "Ford",
            Model = "F-150",
            Plate = "ABC-111"
        });

        vehicle.Plate = "XYZ-999";
        await service.UpdateVehicleAsync(vehicle);

        (await service.GetByIdAsync(tenant.Id))!.Vehicles.Should().ContainSingle(v => v.Plate == "XYZ-999");
    }
}
