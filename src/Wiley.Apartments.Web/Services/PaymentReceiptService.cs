using System.Security.Claims;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Wiley.Apartments.Contracts;
using Wiley.Apartments.Domain;
using Wiley.Apartments.Web.Data;

namespace Wiley.Apartments.Web.Services;

public sealed class PaymentReceiptService(
    ApartmentsDbContext db,
    ILedgerService ledger,
    IDocumentService documents,
    IDateTimeService clock,
    PaymentReceiptGenerator generator,
    IHttpContextAccessor httpContextAccessor,
    ILogger<PaymentReceiptService> logger) : IPaymentReceiptService
{
    private readonly ApartmentsDbContext _db = db;
    private readonly ILedgerService _ledger = ledger;
    private readonly IDocumentService _documents = documents;
    private readonly IDateTimeService _clock = clock;
    private readonly PaymentReceiptGenerator _generator = generator;
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
    private readonly ILogger<PaymentReceiptService> _logger = logger;

    public async Task<PaymentReceiptResult> GenerateAsync(
        Guid paymentEntryId,
        bool saveToVault = true,
        string uploadedBy = "clerk",
        CancellationToken cancellationToken = default)
    {
        var entry = await _db.LedgerEntries
                        .AsNoTracking()
                        .Include(e => e.Tenant)
                        .Include(e => e.FacilityRenter)
                        .Include(e => e.Unit)
                        .FirstOrDefaultAsync(e => e.Id == paymentEntryId && !e.IsDeleted, cancellationToken)
                    ?? throw new InvalidOperationException($"Ledger entry {paymentEntryId} was not found.");

        if (entry.EntryType != LedgerEntryType.Payment)
        {
            throw new InvalidOperationException("Receipts can only be generated for payment entries.");
        }

        var unit = entry.Unit
                   ?? throw new InvalidOperationException("Payment is missing unit data.");

        string payeeName;
        Guid? vaultEntityId;
        DocumentEntityType vaultEntityType;
        decimal balance;
        if (entry.FacilityRenterId is Guid frId)
        {
            var renter = entry.FacilityRenter
                         ?? await _db.FacilityRenters.AsNoTracking()
                             .FirstOrDefaultAsync(r => r.Id == frId, cancellationToken)
                         ?? throw new InvalidOperationException("Payment is missing facility renter data.");
            payeeName = $"{renter.FirstName} {renter.LastName}".Trim();
            vaultEntityId = frId;
            vaultEntityType = DocumentEntityType.FacilityRenter;
            balance = await _ledger.GetFacilityBalanceAsync(
                frId, entry.FacilityReservationId, cancellationToken);
        }
        else
        {
            var tenant = entry.Tenant
                         ?? throw new InvalidOperationException("Payment is missing tenant data.");
            payeeName = $"{tenant.FirstName} {tenant.LastName}".Trim();
            vaultEntityId = entry.TenantId;
            vaultEntityType = DocumentEntityType.Tenant;
            balance = await _ledger.GetBalanceAsync(entry.TenantId!.Value, entry.UnitId, cancellationToken);
        }

        var localDate = _clock.ToDisplayTime(entry.DateUtc);
        var receiptNumber =
            $"WR-{localDate:yyyy}-{localDate:MMdd}-{paymentEntryId.ToString("N")[..3].ToUpperInvariant()}";

        var clerkName = string.IsNullOrWhiteSpace(uploadedBy) ? "Town Clerk" : uploadedBy.Trim();
        var paymentType = entry.FacilityReservationId is not null
            ? (entry.IsDeposit ? "CC damage deposit" : "Community Center rental")
            : ResolvePaymentType(entry);
        var method = MapPaymentMethod(entry.Method);
        var reference = ExtractReference(entry.Notes, entry.Method);
        var description = BuildDescription(entry, paymentType, localDate);
        var notes = BuildNotes(entry.Notes, balance);

        var pdf = _generator.Generate(new PaymentReceiptData(
            receiptNumber,
            localDate.ToString("MM/dd/yyyy"),
            payeeName,
            unit.Number,
            paymentType,
            entry.Amount.ToString("0.00"),
            method,
            reference,
            description,
            notes,
            clerkName,
            $"/s/ {clerkName}"));

        var fileName = $"receipt-{unit.Number}-{localDate:yyyyMMdd}-{receiptNumber}.pdf";
        Guid? documentId = null;
        if (saveToVault)
        {
            var existing = await _db.Documents.AsNoTracking()
                .Where(d => !d.IsDeleted
                            && d.EntityType == vaultEntityType
                            && d.EntityId == vaultEntityId
                            && d.Category == DocumentCategory.Receipt
                            && d.OriginalFileName == fileName)
                .OrderByDescending(d => d.UploadedAtUtc)
                .FirstOrDefaultAsync(cancellationToken);

            if (existing is not null)
            {
                documentId = existing.Id;
                _logger.LogInformation(
                    "Reusing existing vault receipt {DocumentId} for payment {PaymentId} ({FileName}).",
                    documentId,
                    paymentEntryId,
                    fileName);
            }
            else
            {
                await using var stream = new MemoryStream(pdf);
                var relativeDir = vaultEntityType == DocumentEntityType.FacilityRenter
                    ? $"community-center/renters/{vaultEntityId:N}/receipts"
                    : $"tenants/{vaultEntityId:N}/receipts";
                var doc = await _documents.UploadAsync(
                    vaultEntityType,
                    vaultEntityId!.Value,
                    DocumentCategory.Receipt,
                    fileName,
                    "application/pdf",
                    stream,
                    uploadedBy,
                    relativeDir,
                    cancellationToken);
                documentId = doc.Id;
            }
        }

        await WriteReceiptAuditAsync(
            receiptNumber,
            paymentEntryId,
            vaultEntityId,
            unit.Number,
            entry.Amount,
            fileName,
            documentId,
            saveToVault,
            cancellationToken);

        _logger.LogInformation(
            "Generated payment receipt {ReceiptNumber} for payment {PaymentId} party {PartyId} unit {UnitNumber} vaultDoc={DocumentId}.",
            receiptNumber,
            paymentEntryId,
            vaultEntityId,
            unit.Number,
            documentId);

        return new PaymentReceiptResult(paymentEntryId, receiptNumber, pdf, fileName, documentId);
    }

    private async Task WriteReceiptAuditAsync(
        string receiptNumber,
        Guid paymentEntryId,
        Guid? partyId,
        string unitNumber,
        decimal amount,
        string fileName,
        Guid? documentId,
        bool savedToVault,
        CancellationToken cancellationToken)
    {
        var userId = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? _httpContextAccessor.HttpContext?.User?.Identity?.Name
            ?? "system";

        var after = JsonSerializer.Serialize(new
        {
            receiptNumber,
            paymentEntryId,
            partyId,
            unitNumber,
            amount,
            fileName,
            documentId,
            savedToVault
        });

        _db.AuditLogs.Add(new AuditLog
        {
            UserId = userId,
            TimestampUtc = _clock.UtcNow,
            EntityType = "PaymentReceipt",
            EntityId = paymentEntryId.ToString("N"),
            Action = "Generate",
            OldValues = null,
            NewValues = after
        });

        await _db.SaveChangesAsync(cancellationToken);
    }

    private static string ResolvePaymentType(LedgerEntry entry)
    {
        if (entry.IsDeposit)
        {
            return "Security Deposit";
        }

        var notes = entry.Notes ?? string.Empty;
        if (notes.Contains("late", StringComparison.OrdinalIgnoreCase))
        {
            return "Late Fee";
        }

        if (notes.Contains("pet", StringComparison.OrdinalIgnoreCase) &&
            notes.Contains("deposit", StringComparison.OrdinalIgnoreCase))
        {
            return "Pet Deposit";
        }

        if (notes.Contains("utilit", StringComparison.OrdinalIgnoreCase))
        {
            return "Utility Reimbursement";
        }

        return "Rent";
    }

    private static string MapPaymentMethod(PaymentMethod? method) =>
        method switch
        {
            PaymentMethod.Cash => "Cash",
            PaymentMethod.Check => "Check",
            PaymentMethod.OnlineReference => "Online / ACH",
            PaymentMethod.Other => "Other",
            _ => "Other",
        };

    private static string? ExtractReference(string? notes, PaymentMethod? method)
    {
        if (string.IsNullOrWhiteSpace(notes))
        {
            return null;
        }

        // Common patterns: "Check #4821", "ref 1404", "#4821"
        var trimmed = notes.Trim();
        var hash = trimmed.IndexOf('#');
        if (hash >= 0 && hash < trimmed.Length - 1)
        {
            var token = new string([.. trimmed[(hash + 1)..].TakeWhile(c => char.IsLetterOrDigit(c))]);
            if (!string.IsNullOrEmpty(token))
            {
                return token;
            }
        }

        if (method is PaymentMethod.Check or PaymentMethod.OnlineReference)
        {
            return trimmed.Length <= 40 ? trimmed : trimmed[..40];
        }

        return null;
    }

    private static string BuildDescription(LedgerEntry entry, string paymentType, DateTime localDate)
    {
        if (!string.IsNullOrWhiteSpace(entry.Notes) &&
            !entry.Notes.Contains('#') &&
            entry.Notes.Length <= 80)
        {
            return entry.Notes.Trim();
        }

        return paymentType switch
        {
            "Security Deposit" => "Security deposit payment",
            "Late Fee" => $"Late fee — {localDate:MMMM yyyy}",
            _ => $"Rent for {localDate:MMMM yyyy}",
        };
    }

    private static string BuildNotes(string? entryNotes, decimal balance)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(entryNotes))
        {
            parts.Add(entryNotes.Trim());
        }

        parts.Add(balance == 0m
            ? "Payment received in full. Thank you."
            : $"Ledger balance after payment: {balance:C}.");

        return string.Join(' ', parts);
    }
}
