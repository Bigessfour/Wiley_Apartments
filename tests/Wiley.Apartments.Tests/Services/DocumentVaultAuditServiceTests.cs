using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Wiley.Apartments.Tests.Support;
using Wiley.Apartments.Web.Data;
using Wiley.Apartments.Web.Services;

namespace Wiley.Apartments.Tests.Services;

public sealed class DocumentVaultAuditServiceTests
{
    [Fact]
    public async Task LogAsync_WritesAppendOnlyAuditLog()
    {
        using var dbFactory = new SqliteTestDatabase();
        await using var db = dbFactory.CreateContext();
        var accessor = new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext()
        };
        accessor.HttpContext.User = new System.Security.Claims.ClaimsPrincipal(
            new System.Security.Claims.ClaimsIdentity(
            [
                new System.Security.Claims.Claim(
                    System.Security.Claims.ClaimTypes.NameIdentifier, "clerk-1")
            ], "test"));

        var audit = new DocumentVaultAuditService(db, accessor, NullLogger<DocumentVaultAuditService>.Instance);
        await audit.LogAsync("delete", "/units/1", ["a.pdf"]);

        var row = await db.AuditLogs.SingleAsync();
        row.EntityType.Should().Be("DocumentVaultFile");
        row.Action.Should().Be("delete");
        row.UserId.Should().Be("clerk-1");
        row.OldValues.Should().Contain("a.pdf");
    }
}
