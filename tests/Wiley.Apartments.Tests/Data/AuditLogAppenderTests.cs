using Microsoft.EntityFrameworkCore;
using Wiley.Apartments.Domain;
using Wiley.Apartments.Web.Data;

namespace Wiley.Apartments.Tests.Data;

public class AuditLogAppenderTests
{
    [Fact]
    public void MapEntry_ExcludesSensitiveIdentityFields()
    {
        var options = new DbContextOptionsBuilder<ApartmentsDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;

        using var context = new ApartmentsDbContext(options);
        context.Database.OpenConnection();
        context.Database.EnsureCreated();

        var user = new ApplicationUser
        {
            UserName = "clerk@test.local",
            Email = "clerk@test.local",
            PasswordHash = "hashed-secret",
            SecurityStamp = "stamp-secret"
        };
        var entry = context.Users.Add(user);
        entry.State = EntityState.Added;

        var audit = AuditLogAppender.MapEntry(entry, "clerk@town.gov", DateTime.UtcNow);

        audit.NewValues.Should().NotBeNull();
        audit.NewValues.Should().NotContain("hashed-secret");
        audit.NewValues.Should().NotContain("stamp-secret");
        audit.NewValues.Should().Contain("clerk@test.local");
    }

    [Fact]
    public void MapEntry_CreatesCreateAction_ForAddedEntity()
    {
        var options = new DbContextOptionsBuilder<ApartmentsDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;

        using var context = new ApartmentsDbContext(options);
        var entry = context.Add(new AuditLog
        {
            UserId = "seed",
            TimestampUtc = DateTime.UtcNow,
            EntityType = "Seed",
            EntityId = "1",
            Action = "Create"
        });
        entry.State = EntityState.Added;

        var audit = AuditLogAppender.MapEntry(entry, "clerk@town.gov", DateTime.UtcNow);

        audit.UserId.Should().Be("clerk@town.gov");
        audit.Action.Should().Be("Create");
        audit.EntityType.Should().Be(nameof(AuditLog));
        audit.NewValues.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void CreateEntries_SkipsUnchangedEntities()
    {
        var options = new DbContextOptionsBuilder<ApartmentsDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;

        using var context = new ApartmentsDbContext(options);
        context.Database.OpenConnection();
        context.Database.EnsureCreated();

        var log = new AuditLog
        {
            UserId = "system",
            TimestampUtc = DateTime.UtcNow,
            EntityType = "Test",
            EntityId = "1",
            Action = "Create"
        };
        context.AuditLogs.Add(log);
        context.SaveChanges();

        context.Entry(log).State = EntityState.Unchanged;
        var entries = AuditLogAppender.CreateEntries(context, "clerk@town.gov", DateTime.UtcNow);

        entries.Should().BeEmpty();
    }
}
