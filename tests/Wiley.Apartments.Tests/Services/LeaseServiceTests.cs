using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Wiley.Apartments.Domain;
using Wiley.Apartments.Web.Configuration;
using Wiley.Apartments.Web.Data;
using Wiley.Apartments.Web.Services;

namespace Wiley.Apartments.Tests.Services;

public class LeaseServiceTests
{
    private sealed class FixedClock : Wiley.Apartments.Contracts.IDateTimeService
    {
        public DateTime UtcNow { get; set; } = new(2026, 8, 9, 18, 0, 0, DateTimeKind.Utc);
        public DateTime ToDisplayTime(DateTime utc) => utc;
        public DateTime ToUtc(DateTime local) => DateTime.SpecifyKind(local, DateTimeKind.Utc);
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "tests";
        public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; }
            = new Microsoft.Extensions.FileProviders.NullFileProvider();
    }

    private static string? ResolveTemplatesDir()
    {
        var candidates = new[]
        {
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "local-docs")),
            Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "local-docs")),
            Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "local-docs"))
        };

        return candidates.FirstOrDefault(d =>
            File.Exists(Path.Combine(d, "templates", "brookside-year-lease.docx")));
    }

    private static (ApartmentsDbContext Db, LeaseService Service, string DocRoot) Create(string documentRoot)
    {
        var connection = new Microsoft.Data.Sqlite.SqliteConnection("Data Source=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<ApartmentsDbContext>()
            .UseSqlite(connection)
            .Options;
        var db = new ApartmentsDbContext(options);
        db.Database.EnsureCreated();

        var env = new TestHostEnvironment { ContentRootPath = Path.GetTempPath() };
        var opts = Options.Create(new ClerkSuiteOptions { DocumentRoot = documentRoot });
        var service = new LeaseService(
            db,
            opts,
            env,
            new FixedClock(),
            new LeaseDocumentGenerator(),
            NullLogger<LeaseService>.Instance);
        return (db, service, documentRoot);
    }

    [Fact]
    public async Task ListTemplatesAsync_ReturnsBrooksideFiles_WhenPresent()
    {
        var root = ResolveTemplatesDir();
        if (root is null)
        {
            return;
        }

        var (db, service, _) = Create(root);
        await using (db)
        {
            var templates = await service.ListTemplatesAsync();
            templates.Should().HaveCount(2);
            templates.Select(t => Path.GetFileNameWithoutExtension(t.FileName))
                .Should().Contain(["brookside-year-lease", "brookside-month-to-month-lease"]);
            templates.Should().OnlyContain(t =>
                t.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase)
                || t.FileName.EndsWith(".docx", StringComparison.OrdinalIgnoreCase));
        }
    }

    [Fact]
    public async Task CreateDraftAndGenerate_WritesPdf_AndStaysDraft()
    {
        var root = ResolveTemplatesDir();
        if (root is null)
        {
            return;
        }

        var workRoot = Path.Combine(Path.GetTempPath(), "clerksuite-lease-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(workRoot, "templates"));
        File.Copy(
            Path.Combine(root, "templates", "brookside-year-lease.docx"),
            Path.Combine(workRoot, "templates", "brookside-year-lease.docx"));

        var (db, service, _) = Create(workRoot);
        await using (db)
        {
            var unit = new Unit { Id = Guid.NewGuid(), Number = "3", SqFt = 700, Beds = 2, Baths = 1 };
            var tenant = new Tenant
            {
                Id = Guid.NewGuid(),
                FirstName = "Pat",
                LastName = "Nguyen",
                Phone = "719-555-0100"
            };
            db.Units.Add(unit);
            db.Tenants.Add(tenant);
            await db.SaveChangesAsync();

            var templates = await service.ListTemplatesAsync();
            templates.Should().Contain(t => t.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase));

            var draft = await service.CreateDraftAsync(
                unit.Id,
                tenant.Id,
                "brookside-year-lease.pdf",
                new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2027, 8, 31, 0, 0, 0, DateTimeKind.Utc),
                650m,
                650m);

            draft.Status.Should().Be(LeaseStatus.Draft);

            var lease = await service.GenerateDocumentsAsync(draft.Id);

            lease.Status.Should().Be(LeaseStatus.Draft);
            lease.GeneratedPdfRelativePath.Should().NotBeNullOrWhiteSpace();
            File.Exists(Path.Combine(workRoot, lease.GeneratedPdfRelativePath!)).Should().BeTrue();
            File.Exists(Path.Combine(workRoot, "templates", "brookside-year-lease.pdf")).Should().BeTrue();
        }

        try
        {
            Directory.Delete(workRoot, recursive: true);
        }
        catch
        {
            // best-effort cleanup
        }
    }

    [Fact]
    public async Task SoftDeleteAsync_HidesFromGetAll()
    {
        var root = ResolveTemplatesDir() ?? Path.Combine(Path.GetTempPath(), "empty-docs-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "templates"));
        // Ensure year template exists so CreateDraft can proceed when root was a temp empty dir.
        var yearSrc = ResolveTemplatesDir();
        if (yearSrc is null)
        {
            return;
        }

        if (!File.Exists(Path.Combine(root, "templates", "brookside-year-lease.docx")))
        {
            File.Copy(
                Path.Combine(yearSrc, "templates", "brookside-year-lease.docx"),
                Path.Combine(root, "templates", "brookside-year-lease.docx"));
        }

        var (db, service, _) = Create(root);
        await using (db)
        {
            var unit = new Unit { Id = Guid.NewGuid(), Number = "1", SqFt = 500, Beds = 1, Baths = 1 };
            var tenant = new Tenant { Id = Guid.NewGuid(), FirstName = "A", LastName = "B" };
            db.Units.Add(unit);
            db.Tenants.Add(tenant);
            await db.SaveChangesAsync();

            var draft = await service.CreateDraftAsync(
                unit.Id,
                tenant.Id,
                "brookside-year-lease.docx",
                DateTime.UtcNow,
                DateTime.UtcNow.AddMonths(12),
                100m,
                100m);

            await service.SoftDeleteAsync(draft.Id);

            (await service.GetAllAsync()).Should().BeEmpty();
            (await service.GetByIdAsync(draft.Id)).Should().BeNull();
        }
    }
}
