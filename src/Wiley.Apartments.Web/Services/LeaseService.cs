using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Wiley.Apartments.Contracts;
using Wiley.Apartments.Domain;
using Wiley.Apartments.Web.Configuration;
using Wiley.Apartments.Web.Data;

namespace Wiley.Apartments.Web.Services;

public sealed class LeaseService : ILeaseService
{
    private static readonly (string BaseName, string DisplayName, string Term)[] KnownTemplates =
    [
        ("brookside-year-lease", "Brookside — year lease", "One calendar year (auto-renew yearly)"),
        ("brookside-month-to-month-lease", "Brookside — month-to-month", "One calendar month (auto-renew monthly)")
    ];

    private readonly ApartmentsDbContext _db;
    private readonly ClerkSuiteOptions _options;
    private readonly IDocumentPathResolver _paths;
    private readonly IHostEnvironment _environment;
    private readonly IDateTimeService _clock;
    private readonly LeaseDocumentGenerator _generator;
    private readonly IDocumentService _documents;
    private readonly ILogger<LeaseService> _logger;

    public LeaseService(
        ApartmentsDbContext db,
        IOptions<ClerkSuiteOptions> options,
        IDocumentPathResolver paths,
        IHostEnvironment environment,
        IDateTimeService clock,
        LeaseDocumentGenerator generator,
        IDocumentService documents,
        ILogger<LeaseService> logger)
    {
        _db = db;
        _options = options.Value;
        _paths = paths;
        _environment = environment;
        _clock = clock;
        _generator = generator;
        _documents = documents;
        _logger = logger;
    }

    public Task<IReadOnlyList<LeaseTemplateInfo>> ListTemplatesAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        EnsureFillablePdfTemplates();
        var templatesDir = Path.Combine(ResolveDocumentRoot(), "templates");
        var list = new List<LeaseTemplateInfo>();

        foreach (var (baseName, displayName, term) in KnownTemplates)
        {
            var pdfName = $"{baseName}.pdf";
            var docxName = $"{baseName}.docx";
            var pdfPath = Path.Combine(templatesDir, pdfName);
            var docxPath = Path.Combine(templatesDir, docxName);

            if (File.Exists(pdfPath))
            {
                list.Add(new LeaseTemplateInfo(
                    pdfName,
                    displayName + " (fillable PDF)",
                    Path.Combine("templates", pdfName).Replace('\\', '/'),
                    term));
            }
            else if (File.Exists(docxPath))
            {
                list.Add(new LeaseTemplateInfo(
                    docxName,
                    displayName + " (DOCX fallback)",
                    Path.Combine("templates", docxName).Replace('\\', '/'),
                    term));
            }
        }

        return Task.FromResult<IReadOnlyList<LeaseTemplateInfo>>(list);
    }

    public async Task<IReadOnlyList<Lease>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await _db.Leases
            .AsNoTracking()
            .Include(l => l.Unit)
            .Include(l => l.Tenant)
            .Where(l => !l.IsDeleted)
            .OrderByDescending(l => l.StartUtc)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Lease>> GetByTenantAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default) =>
        await _db.Leases
            .AsNoTracking()
            .Include(l => l.Unit)
            .Include(l => l.Tenant)
            .Where(l => !l.IsDeleted && l.TenantId == tenantId)
            .OrderByDescending(l => l.StartUtc)
            .ToListAsync(cancellationToken);

    public async Task<Lease?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _db.Leases
            .Include(l => l.Unit)
            .Include(l => l.Tenant)
            .FirstOrDefaultAsync(l => l.Id == id && !l.IsDeleted, cancellationToken);

    public async Task<Lease> CreateDraftAsync(
        Guid unitId,
        Guid tenantId,
        string templateFileName,
        DateTime startUtc,
        DateTime endUtc,
        decimal rent,
        decimal deposit,
        string? customClauses = null,
        CancellationToken cancellationToken = default)
    {
        EnsureFillablePdfTemplates();

        if (string.IsNullOrWhiteSpace(templateFileName))
        {
            throw new ArgumentException("Template file name is required.", nameof(templateFileName));
        }

        var baseName = Path.GetFileNameWithoutExtension(templateFileName);
        if (KnownTemplates.All(t => t.BaseName != baseName))
        {
            throw new ArgumentException("Unknown or unsupported lease template.", nameof(templateFileName));
        }

        _ = await _db.Units.FindAsync([unitId], cancellationToken)
            ?? throw new InvalidOperationException($"Unit {unitId} was not found.");
        var tenant = await _db.Tenants.FindAsync([tenantId], cancellationToken)
            ?? throw new InvalidOperationException($"Tenant {tenantId} was not found.");
        if (tenant.IsDeleted)
        {
            throw new InvalidOperationException("Cannot create a lease for a soft-deleted tenant.");
        }

        if (endUtc <= startUtc)
        {
            throw new ArgumentException("Lease end must be after start.");
        }

        if (rent < 0 || deposit < 0)
        {
            throw new ArgumentException("Rent and deposit cannot be negative.");
        }

        var templates = await ListTemplatesAsync(cancellationToken);
        var selected = templates.FirstOrDefault(t =>
            t.FileName.Equals(templateFileName, StringComparison.OrdinalIgnoreCase)
            || Path.GetFileNameWithoutExtension(t.FileName)
                .Equals(baseName, StringComparison.OrdinalIgnoreCase));
        if (selected is null)
        {
            throw new InvalidOperationException(
                $"Template '{templateFileName}' not found under DocumentRoot/templates. See deploy/synology/TEMPLATES.md.");
        }

        var clauses = string.IsNullOrWhiteSpace(customClauses) ? null : customClauses.Trim();
        if (clauses is { Length: > 4000 })
        {
            throw new ArgumentException("Custom clauses cannot exceed 4000 characters.", nameof(customClauses));
        }

        var lease = new Lease
        {
            Id = Guid.NewGuid(),
            UnitId = unitId,
            TenantId = tenantId,
            StartUtc = EnsureUtc(startUtc),
            EndUtc = EnsureUtc(endUtc),
            Rent = rent,
            Deposit = deposit,
            Status = LeaseStatus.Draft,
            TemplateUsed = selected.FileName,
            CustomClauses = clauses,
            IsDeleted = false
        };

        _db.Leases.Add(lease);
        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Created draft lease {LeaseId} using {Template}.", lease.Id, selected.FileName);
        return (await GetByIdAsync(lease.Id, cancellationToken))!;
    }

    public async Task<Lease> GenerateDocumentsAsync(
        Guid leaseId,
        CancellationToken cancellationToken = default)
    {
        EnsureFillablePdfTemplates();

        var lease = await _db.Leases
            .Include(l => l.Unit)
            .Include(l => l.Tenant)!
            .ThenInclude(t => t!.HouseholdMembers)
            .FirstOrDefaultAsync(l => l.Id == leaseId && !l.IsDeleted, cancellationToken)
            ?? throw new InvalidOperationException($"Lease {leaseId} was not found.");

        if (lease.Unit is null || lease.Tenant is null)
        {
            throw new InvalidOperationException("Lease is missing unit or tenant.");
        }

        var templatePath = Path.Combine(ResolveDocumentRoot(), "templates", lease.TemplateUsed);
        if (!File.Exists(templatePath))
        {
            // Prefer PDF sibling if draft still points at docx after bootstrap.
            var pdfSibling = Path.ChangeExtension(templatePath, ".pdf");
            if (File.Exists(pdfSibling))
            {
                templatePath = pdfSibling;
                lease.TemplateUsed = Path.GetFileName(pdfSibling);
            }
            else
            {
                throw new InvalidOperationException($"Template file missing: {templatePath}");
            }
        }

        var merge = BuildMergeData(lease);

        // Prefer DOCX merge (underscore replace → PDF). Fillable PDF templates currently keep
        // @@markers@@ in page content, so AcroForm fill alone produces unreadable leases.
        var docxSibling = Path.ChangeExtension(templatePath, ".docx");
        byte[]? docx;
        byte[] pdf;
        if (templatePath.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase)
            && File.Exists(docxSibling))
        {
            await using var docxStream = File.OpenRead(docxSibling);
            (docx, pdf) = _generator.GenerateFromDocx(docxStream, merge);
            lease.TemplateUsed = Path.GetFileName(docxSibling);
        }
        else
        {
            await using var templateStream = File.OpenRead(templatePath);
            (docx, pdf) = _generator.Generate(templateStream, Path.GetFileName(templatePath), merge);
        }

        if (pdf.AsSpan().IndexOf("Created with a trial version of Syncfusion"u8) >= 0)
        {
            throw new InvalidOperationException(
                "Generated lease PDF contains a Syncfusion trial watermark. " +
                "Register a SYNCFUSION_LICENSE_KEY that includes PDF/Word (not Blazor-only) via user-secrets.");
        }

        var leasesDir = Path.Combine(ResolveDocumentRoot(), "leases");
        Directory.CreateDirectory(leasesDir);
        var stamp = _clock.UtcNow.ToString("yyyyMMddHHmmss");
        var baseName = $"lease-{lease.Unit.Number}-{lease.Id:N}-{stamp}";
        var pdfRel = Path.Combine("leases", $"{baseName}.pdf").Replace('\\', '/');
        await File.WriteAllBytesAsync(Path.Combine(ResolveDocumentRoot(), pdfRel), pdf, cancellationToken);
        lease.GeneratedPdfRelativePath = pdfRel;

        if (docx is not null)
        {
            var docxRel = Path.Combine("leases", $"{baseName}.docx").Replace('\\', '/');
            await File.WriteAllBytesAsync(Path.Combine(ResolveDocumentRoot(), docxRel), docx, cancellationToken);
            lease.GeneratedDocxRelativePath = docxRel;
        }

        // Stay Draft until signed upload / activate workflow (T3.3 / T3.4).
        lease.Status = LeaseStatus.Draft;
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Generated lease PDF for {LeaseId}: {Pdf}", lease.Id, pdfRel);
        return (await GetByIdAsync(lease.Id, cancellationToken))!;
    }

    public async Task<Lease> AttachSignedDocumentAsync(
        Guid leaseId,
        string originalFileName,
        string contentType,
        Stream content,
        string uploadedBy,
        CancellationToken cancellationToken = default)
    {
        var lease = await _db.Leases
            .Include(l => l.Unit)
            .FirstOrDefaultAsync(l => l.Id == leaseId && !l.IsDeleted, cancellationToken)
            ?? throw new InvalidOperationException($"Lease {leaseId} was not found.");

        if (lease.Unit is null)
        {
            throw new InvalidOperationException("Lease is missing unit.");
        }

        var ext = Path.GetExtension(originalFileName);
        if (!ext.Equals(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Signed lease must be a PDF.");
        }

        var relativeDir = Path.Combine("leases", lease.Unit.Number, "signed").Replace('\\', '/');
        var doc = await _documents.UploadAsync(
            DocumentEntityType.Lease,
            lease.Id,
            DocumentCategory.SignedLease,
            originalFileName,
            contentType,
            content,
            uploadedBy,
            relativeDir,
            cancellationToken);

        lease.SignedDocumentId = doc.Id;
        lease.Status = LeaseStatus.Active;
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Attached signed lease document {DocumentId} to lease {LeaseId}; status Active.",
            doc.Id, lease.Id);
        return (await GetByIdAsync(lease.Id, cancellationToken))!;
    }

    public async Task<IReadOnlyList<Lease>> GetExpiringWithinAsync(
        int withinDays,
        CancellationToken cancellationToken = default)
    {
        if (withinDays < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(withinDays));
        }

        var now = _clock.UtcNow;
        var until = now.AddDays(withinDays);
        return await _db.Leases
            .AsNoTracking()
            .Include(l => l.Unit)
            .Include(l => l.Tenant)
            .Where(l => !l.IsDeleted
                && (l.Status == LeaseStatus.Active || l.Status == LeaseStatus.Amended)
                && l.EndUtc >= now
                && l.EndUtc <= until)
            .OrderBy(l => l.EndUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<Lease> AmendAsync(
        Guid leaseId,
        decimal? rent = null,
        decimal? deposit = null,
        DateTime? endUtc = null,
        string? customClauses = null,
        string? note = null,
        CancellationToken cancellationToken = default)
    {
        var lease = await RequireLifecycleLeaseAsync(leaseId, cancellationToken);
        if (rent is < 0 || deposit is < 0)
        {
            throw new ArgumentException("Rent and deposit cannot be negative.");
        }

        if (endUtc is not null)
        {
            var end = EnsureUtc(endUtc.Value);
            if (end <= lease.StartUtc)
            {
                throw new ArgumentException("Amended end must be after lease start.");
            }

            lease.EndUtc = end;
        }

        if (rent is not null)
        {
            lease.Rent = rent.Value;
        }

        if (deposit is not null)
        {
            lease.Deposit = deposit.Value;
        }

        if (customClauses is not null)
        {
            var clauses = string.IsNullOrWhiteSpace(customClauses) ? null : customClauses.Trim();
            if (clauses is { Length: > 4000 })
            {
                throw new ArgumentException("Custom clauses cannot exceed 4000 characters.");
            }

            lease.CustomClauses = clauses;
        }

        lease.Status = LeaseStatus.Amended;
        lease.LifecycleNote = TrimNote(note) ?? lease.LifecycleNote;
        ConcurrencyHelper.BumpRowVersion(lease);
        await ConcurrencyHelper.SaveChangesOrThrowAsync(_db, "Lease", cancellationToken);
        _logger.LogInformation("Amended lease {LeaseId}.", lease.Id);
        return (await GetByIdAsync(lease.Id, cancellationToken))!;
    }

    public async Task<Lease> RenewAsync(
        Guid leaseId,
        DateTime newEndUtc,
        decimal? rent = null,
        decimal? deposit = null,
        string? note = null,
        CancellationToken cancellationToken = default)
    {
        var prior = await RequireLifecycleLeaseAsync(leaseId, cancellationToken);
        var newEnd = EnsureUtc(newEndUtc);
        var newStart = prior.EndUtc;
        if (newEnd <= newStart)
        {
            throw new ArgumentException("Renewal end must be after the prior lease end (new start).");
        }

        if (rent is < 0 || deposit is < 0)
        {
            throw new ArgumentException("Rent and deposit cannot be negative.");
        }

        var successor = new Lease
        {
            Id = Guid.NewGuid(),
            UnitId = prior.UnitId,
            TenantId = prior.TenantId,
            StartUtc = newStart,
            EndUtc = newEnd,
            Rent = rent ?? prior.Rent,
            Deposit = deposit ?? prior.Deposit,
            Status = LeaseStatus.Draft,
            TemplateUsed = prior.TemplateUsed,
            CustomClauses = prior.CustomClauses,
            PriorLeaseId = prior.Id,
            IsDeleted = false,
            LifecycleNote = TrimNote(note)
        };

        prior.Status = LeaseStatus.Renewed;
        prior.SuccessorLeaseId = successor.Id;
        prior.LifecycleNote = TrimNote(note) ?? prior.LifecycleNote;
        ConcurrencyHelper.BumpRowVersion(prior);

        _db.Leases.Add(successor);
        await ConcurrencyHelper.SaveChangesOrThrowAsync(_db, "Lease", cancellationToken);
        _logger.LogInformation(
            "Renewed lease {PriorId} → successor draft {SuccessorId}.",
            prior.Id, successor.Id);
        return (await GetByIdAsync(successor.Id, cancellationToken))!;
    }

    public async Task<Lease> TerminateAsync(
        Guid leaseId,
        DateTime effectiveEndUtc,
        string? note = null,
        CancellationToken cancellationToken = default)
    {
        var lease = await RequireLifecycleLeaseAsync(leaseId, cancellationToken);
        var effective = EnsureUtc(effectiveEndUtc);
        if (effective < lease.StartUtc)
        {
            throw new ArgumentException("Termination date cannot be before lease start.");
        }

        if (effective < lease.EndUtc)
        {
            lease.EndUtc = effective;
        }

        lease.Status = LeaseStatus.Terminated;
        lease.LifecycleNote = TrimNote(note) ?? lease.LifecycleNote;
        ConcurrencyHelper.BumpRowVersion(lease);
        await ConcurrencyHelper.SaveChangesOrThrowAsync(_db, "Lease", cancellationToken);
        _logger.LogInformation("Terminated lease {LeaseId} effective {End}.", lease.Id, lease.EndUtc);
        return (await GetByIdAsync(lease.Id, cancellationToken))!;
    }

    public async Task SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var lease = await _db.Leases.FindAsync([id], cancellationToken)
            ?? throw new InvalidOperationException($"Lease {id} was not found.");
        lease.IsDeleted = true;
        ConcurrencyHelper.BumpRowVersion(lease);
        await ConcurrencyHelper.SaveChangesOrThrowAsync(_db, "Lease", cancellationToken);
    }

    private async Task<Lease> RequireLifecycleLeaseAsync(Guid leaseId, CancellationToken cancellationToken)
    {
        var lease = await _db.Leases
            .FirstOrDefaultAsync(l => l.Id == leaseId && !l.IsDeleted, cancellationToken)
            ?? throw new InvalidOperationException($"Lease {leaseId} was not found.");

        if (lease.Status is not (LeaseStatus.Active or LeaseStatus.Amended))
        {
            throw new InvalidOperationException(
                $"Lease must be Active or Amended to renew/amend/terminate (current: {lease.Status}).");
        }

        return lease;
    }

    private static string? TrimNote(string? note)
    {
        if (string.IsNullOrWhiteSpace(note))
        {
            return null;
        }

        var trimmed = note.Trim();
        return trimmed.Length > 2000 ? trimmed[..2000] : trimmed;
    }

    /// <summary>
    /// When Brookside DOCX exists without a fillable PDF sibling, create AcroForm PDF templates once.
    /// </summary>
    private void EnsureFillablePdfTemplates()
    {
        var templatesDir = Path.Combine(ResolveDocumentRoot(), "templates");
        if (!Directory.Exists(templatesDir))
        {
            return;
        }

        foreach (var (baseName, _, _) in KnownTemplates)
        {
            var docxPath = Path.Combine(templatesDir, $"{baseName}.docx");
            var pdfPath = Path.Combine(templatesDir, $"{baseName}.pdf");
            if (!File.Exists(docxPath))
            {
                continue;
            }

            var needsRebuild = !File.Exists(pdfPath);
            if (!needsRebuild)
            {
                try
                {
                    var existing = File.ReadAllBytes(pdfPath);
                    needsRebuild = existing.AsSpan().IndexOf("@@"u8) >= 0
                        || existing.AsSpan().IndexOf("Created with a trial version of Syncfusion"u8) >= 0;
                }
                catch
                {
                    needsRebuild = true;
                }
            }

            if (!needsRebuild)
            {
                continue;
            }

            try
            {
                using var docx = File.OpenRead(docxPath);
                var fillable = _generator.CreateFillablePdfFromDocx(docx);
                File.WriteAllBytes(pdfPath, fillable);
                _logger.LogInformation("Created/refreshed fillable PDF template {Pdf} from {Docx}.", pdfPath, docxPath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not bootstrap fillable PDF for {BaseName}.", baseName);
            }
        }
    }

    private LeaseMergeData BuildMergeData(Lease lease)
    {
        var tenant = lease.Tenant!;
        var unit = lease.Unit!;
        var household = string.Join(", ",
            tenant.HouseholdMembers.Select(m => m.FullName).Where(n => !string.IsNullOrWhiteSpace(n)));
        if (string.IsNullOrWhiteSpace(household))
        {
            household = $"{tenant.FirstName} {tenant.LastName}".Trim();
        }

        var startLocal = _clock.ToDisplayTime(lease.StartUtc);
        var endLocal = _clock.ToDisplayTime(lease.EndUtc);

        return new LeaseMergeData
        {
            AgreementDate = _clock.ToDisplayTime(_clock.UtcNow),
            PremisesAddress = $"Unit {unit.Number}, Brookside Community Living, Wiley, CO",
            ResidentName = $"{tenant.FirstName} {tenant.LastName}".Trim(),
            HouseholdMembers = household,
            PostOfficeBox = string.Empty,
            PhoneNumber = tenant.Phone,
            ApartmentNumber = unit.Number,
            LeaseStart = startLocal,
            LeaseEnd = endLocal,
            MonthlyRent = lease.Rent,
            RentStart = startLocal,
            SecurityDeposit = lease.Deposit,
            CustomClauses = lease.CustomClauses
        };
    }

    private string ResolveDocumentRoot() => _paths.GetDocumentRoot();

    private static DateTime EnsureUtc(DateTime value) =>
        value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
}
