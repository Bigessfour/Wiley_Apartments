using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Wiley.Apartments.Domain;
using Wiley.Apartments.Web.Data;

namespace Wiley.Apartments.Tests.Data;

public class AuditLogAppendOnlyTests
{
    [Fact]
    public void EnforceAuditLogAppendOnly_Throws_OnModified()
    {
        var options = new DbContextOptionsBuilder<ApartmentsDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;
        using var context = new ApartmentsDbContext(options);
        context.Database.OpenConnection();
        context.Database.EnsureCreated();

        var log = new AuditLog
        {
            UserId = "clerk",
            TimestampUtc = DateTime.UtcNow,
            EntityType = "Unit",
            EntityId = "1",
            Action = "Create"
        };
        context.AuditLogs.Add(log);
        context.SaveChanges();

        log.Action = "Tamper";
        context.Entry(log).State = EntityState.Modified;

        var act = () => AuditSaveChangesInterceptor.EnforceAuditLogAppendOnly(context);
        act.Should().Throw<InvalidOperationException>().WithMessage("*append-only*");
    }

    [Fact]
    public void EnforceAuditLogAppendOnly_Throws_OnDeleted()
    {
        var options = new DbContextOptionsBuilder<ApartmentsDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;
        using var context = new ApartmentsDbContext(options);
        context.Database.OpenConnection();
        context.Database.EnsureCreated();

        var log = new AuditLog
        {
            UserId = "clerk",
            TimestampUtc = DateTime.UtcNow,
            EntityType = "Unit",
            EntityId = "1",
            Action = "Create"
        };
        context.AuditLogs.Add(log);
        context.SaveChanges();

        context.AuditLogs.Remove(log);

        var act = () => AuditSaveChangesInterceptor.EnforceAuditLogAppendOnly(context);
        act.Should().Throw<InvalidOperationException>().WithMessage("*append-only*");
    }

    [Fact]
    public async Task SaveChanges_RejectsAuditLogDelete_ViaInterceptor()
    {
        var options = new DbContextOptionsBuilder<ApartmentsDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;
        var accessor = new HttpContextAccessor { HttpContext = new DefaultHttpContext() };
        await using var context = new ApartmentsDbContext(options);
        context.Database.OpenConnection();
        context.Database.EnsureCreated();

        // Wire interceptor the same way the app does for this test path.
        var interceptor = new AuditSaveChangesInterceptor(accessor);
        var optionsWith = new DbContextOptionsBuilder<ApartmentsDbContext>()
            .UseSqlite(context.Database.GetDbConnection())
            .AddInterceptors(interceptor)
            .Options;
        await using var audited = new ApartmentsDbContext(optionsWith);

        var log = new AuditLog
        {
            UserId = "clerk",
            TimestampUtc = DateTime.UtcNow,
            EntityType = "Tenant",
            EntityId = Guid.NewGuid().ToString("N"),
            Action = "Create"
        };
        audited.AuditLogs.Add(log);
        await audited.SaveChangesAsync();

        audited.AuditLogs.Remove(log);
        var act = () => audited.SaveChangesAsync();
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*append-only*");
    }
}
