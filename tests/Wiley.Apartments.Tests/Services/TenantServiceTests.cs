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
}
