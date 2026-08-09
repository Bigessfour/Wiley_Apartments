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
        var clock = new FixedClock();
        var documents = new DocumentService(
            db,
            opts,
            env,
            clock,
            NullLogger<DocumentService>.Instance);
        var service = new LeaseService(
            db,
            opts,
            env,
            clock,
            new LeaseDocumentGenerator(),
            documents,
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
    public async Task AttachSignedDocumentAsync_StoresVaultFile_AndSetsActive()
    {
        var root = ResolveTemplatesDir();
        if (root is null)
        {
            return;
        }

        var workRoot = Path.Combine(Path.GetTempPath(), "clerksuite-signed-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(workRoot, "templates"));
        File.Copy(
            Path.Combine(root, "templates", "brookside-year-lease.docx"),
            Path.Combine(workRoot, "templates", "brookside-year-lease.docx"));

        var (db, service, _) = Create(workRoot);
        await using (db)
        {
            var unit = new Unit { Id = Guid.NewGuid(), Number = "7", SqFt = 600, Beds = 1, Baths = 1 };
            var tenant = new Tenant { Id = Guid.NewGuid(), FirstName = "Pat", LastName = "Nguyen" };
            db.Units.Add(unit);
            db.Tenants.Add(tenant);
            await db.SaveChangesAsync();

            var draft = await service.CreateDraftAsync(
                unit.Id,
                tenant.Id,
                "brookside-year-lease.pdf",
                DateTime.UtcNow,
                DateTime.UtcNow.AddYears(1),
                500m,
                500m);

            var pdfBytes = "%PDF-1.4 signed stub"u8.ToArray();
            await using var stream = new MemoryStream(pdfBytes);
            var active = await service.AttachSignedDocumentAsync(
                draft.Id,
                "signed-lease.pdf",
                "application/pdf",
                stream,
                "clerk@test");

            active.Status.Should().Be(LeaseStatus.Active);
            active.SignedDocumentId.Should().NotBeNull();
            var doc = await db.Documents.AsNoTracking().SingleAsync(d => d.Id == active.SignedDocumentId);
            doc.Category.Should().Be(DocumentCategory.SignedLease);
            doc.EntityType.Should().Be(DocumentEntityType.Lease);
            File.Exists(Path.Combine(workRoot, doc.FilePathOnNas.Replace('/', Path.DirectorySeparatorChar)))
                .Should().BeTrue();
        }

        try { Directory.Delete(workRoot, recursive: true); } catch { /* ignore */ }
    }

    private static async Task<(Unit Unit, Tenant Tenant, Lease Active)> SeedActiveLeaseAsync(
        ApartmentsDbContext db,
        FixedClock clock,
        int daysUntilEnd = 60,
        string? unitNumber = null)
    {
        var unit = new Unit
        {
            Id = Guid.NewGuid(),
            Number = unitNumber ?? Guid.NewGuid().ToString("N")[..6],
            SqFt = 550,
            Beds = 1,
            Baths = 1
        };
        var tenant = new Tenant { Id = Guid.NewGuid(), FirstName = "Lee", LastName = "Ortiz" };
        var lease = new Lease
        {
            Id = Guid.NewGuid(),
            UnitId = unit.Id,
            TenantId = tenant.Id,
            StartUtc = clock.UtcNow.AddMonths(-6),
            EndUtc = clock.UtcNow.AddDays(daysUntilEnd),
            Rent = 700m,
            Deposit = 700m,
            Status = LeaseStatus.Active,
            TemplateUsed = "brookside-year-lease.pdf"
        };
        db.Units.Add(unit);
        db.Tenants.Add(tenant);
        db.Leases.Add(lease);
        await db.SaveChangesAsync();
        return (unit, tenant, lease);
    }

    [Fact]
    public async Task AmendAsync_UpdatesTerms_AndSetsAmended()
    {
        var root = Path.Combine(Path.GetTempPath(), "clerksuite-lifecycle-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "templates"));
        var (db, service, _) = Create(root);
        await using (db)
        {
            var clock = new FixedClock();
            var (_, _, active) = await SeedActiveLeaseAsync(db, clock);
            var amended = await service.AmendAsync(
                active.Id,
                rent: 750m,
                deposit: null,
                endUtc: active.EndUtc.AddMonths(1),
                customClauses: "No subletting.",
                note: "Rent bump");

            amended.Status.Should().Be(LeaseStatus.Amended);
            amended.Rent.Should().Be(750m);
            amended.CustomClauses.Should().Be("No subletting.");
            amended.LifecycleNote.Should().Be("Rent bump");
        }

        try { Directory.Delete(root, recursive: true); } catch { /* ignore */ }
    }

    [Fact]
    public async Task RenewAsync_CreatesDraftSuccessor_AndMarksPriorRenewed()
    {
        var root = Path.Combine(Path.GetTempPath(), "clerksuite-renew-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "templates"));
        var (db, service, _) = Create(root);
        await using (db)
        {
            var clock = new FixedClock();
            var (_, _, active) = await SeedActiveLeaseAsync(db, clock);
            var newEnd = active.EndUtc.AddYears(1);
            var successor = await service.RenewAsync(active.Id, newEnd, rent: 725m, note: "Annual renew");

            successor.Status.Should().Be(LeaseStatus.Draft);
            successor.PriorLeaseId.Should().Be(active.Id);
            successor.StartUtc.Should().Be(active.EndUtc);
            successor.EndUtc.Should().Be(newEnd);
            successor.Rent.Should().Be(725m);

            var prior = await service.GetByIdAsync(active.Id);
            prior!.Status.Should().Be(LeaseStatus.Renewed);
            prior.SuccessorLeaseId.Should().Be(successor.Id);
        }

        try { Directory.Delete(root, recursive: true); } catch { /* ignore */ }
    }

    [Fact]
    public async Task TerminateAsync_SetsTerminated_AndShortensEnd()
    {
        var root = Path.Combine(Path.GetTempPath(), "clerksuite-term-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "templates"));
        var (db, service, _) = Create(root);
        await using (db)
        {
            var clock = new FixedClock();
            var (_, _, active) = await SeedActiveLeaseAsync(db, clock, daysUntilEnd: 90);
            var effective = clock.UtcNow.AddDays(7);
            var terminated = await service.TerminateAsync(active.Id, effective, "Early move-out");

            terminated.Status.Should().Be(LeaseStatus.Terminated);
            terminated.EndUtc.Should().Be(effective);
            terminated.LifecycleNote.Should().Be("Early move-out");
        }

        try { Directory.Delete(root, recursive: true); } catch { /* ignore */ }
    }

    [Fact]
    public async Task GetExpiringWithinAsync_ReturnsActiveInWindow_Only()
    {
        var root = Path.Combine(Path.GetTempPath(), "clerksuite-exp-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "templates"));
        var (db, service, _) = Create(root);
        await using (db)
        {
            var clock = new FixedClock();
            await SeedActiveLeaseAsync(db, clock, daysUntilEnd: 20, unitNumber: "A20");
            await SeedActiveLeaseAsync(db, clock, daysUntilEnd: 90, unitNumber: "A90");
            var draftUnit = new Unit { Id = Guid.NewGuid(), Number = "2", SqFt = 400, Beds = 1, Baths = 1 };
            var draftTenant = new Tenant { Id = Guid.NewGuid(), FirstName = "X", LastName = "Y" };
            db.Units.Add(draftUnit);
            db.Tenants.Add(draftTenant);
            db.Leases.Add(new Lease
            {
                Id = Guid.NewGuid(),
                UnitId = draftUnit.Id,
                TenantId = draftTenant.Id,
                StartUtc = clock.UtcNow,
                EndUtc = clock.UtcNow.AddDays(10),
                Rent = 1,
                Deposit = 1,
                Status = LeaseStatus.Draft,
                TemplateUsed = "brookside-year-lease.pdf"
            });
            await db.SaveChangesAsync();

            var expiring = await service.GetExpiringWithinAsync(30);
            expiring.Should().HaveCount(1);
            expiring[0].EndUtc.Should().BeCloseTo(clock.UtcNow.AddDays(20), TimeSpan.FromSeconds(1));
        }

        try { Directory.Delete(root, recursive: true); } catch { /* ignore */ }
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
