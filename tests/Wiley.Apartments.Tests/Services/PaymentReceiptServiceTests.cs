using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Wiley.Apartments.Contracts;
using Wiley.Apartments.Domain;
using Wiley.Apartments.Web.Data;
using Wiley.Apartments.Web.Services;

namespace Wiley.Apartments.Tests.Services;

public class PaymentReceiptServiceTests
{
    private sealed class FixedClock : IDateTimeService
    {
        public DateTime UtcNow { get; set; } = new(2026, 8, 10, 18, 0, 0, DateTimeKind.Utc);
        public DateTime ToDisplayTime(DateTime utc) => utc;
        public DateTime ToUtc(DateTime local) => DateTime.SpecifyKind(local, DateTimeKind.Utc);
    }

    [Fact]
    public async Task GenerateAsync_ReusesExistingVaultReceipt_WithoutSecondUpload()
    {
        var connection = new Microsoft.Data.Sqlite.SqliteConnection("Data Source=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<ApartmentsDbContext>()
            .UseSqlite(connection)
            .Options;
        await using var db = new ApartmentsDbContext(options);
        db.Database.EnsureCreated();

        var clock = new FixedClock();
        var unit = new Unit { Id = Guid.NewGuid(), Number = "7B", SqFt = 500, Beds = 1, Baths = 1 };
        var tenant = new Tenant { Id = Guid.NewGuid(), FirstName = "Jane", LastName = "Ramirez" };
        var paymentId = Guid.Parse("01400000-0000-0000-0000-000000000001");
        db.Units.Add(unit);
        db.Tenants.Add(tenant);
        db.LedgerEntries.Add(new LedgerEntry
        {
            Id = paymentId,
            TenantId = tenant.Id,
            UnitId = unit.Id,
            EntryType = LedgerEntryType.Payment,
            Amount = 925m,
            DateUtc = clock.UtcNow,
            Method = PaymentMethod.Check,
            Notes = "Check #4821"
        });

        var localDate = clock.ToDisplayTime(clock.UtcNow);
        var receiptNumber =
            $"WR-{localDate:yyyy}-{localDate:MMdd}-{paymentId.ToString("N")[..3].ToUpperInvariant()}";
        var fileName = $"receipt-{unit.Number}-{localDate:yyyyMMdd}-{receiptNumber}.pdf";
        var existingDocId = Guid.NewGuid();
        db.Documents.Add(new Document
        {
            Id = existingDocId,
            EntityType = DocumentEntityType.Tenant,
            EntityId = tenant.Id,
            Category = DocumentCategory.Receipt,
            OriginalFileName = fileName,
            FilePathOnNas = $"tenants/{tenant.Id:N}/receipts/{fileName}",
            ContentType = "application/pdf",
            UploadedBy = "clerk",
            UploadedAtUtc = clock.UtcNow.AddMinutes(-5)
        });
        await db.SaveChangesAsync();

        var docs = new Mock<IDocumentService>(MockBehavior.Strict);
        var ledger = new Mock<ILedgerService>();
        ledger.Setup(l => l.GetBalanceAsync(tenant.Id, unit.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0m);

        var http = new Mock<IHttpContextAccessor>();
        http.Setup(h => h.HttpContext).Returns((HttpContext?)null);

        var service = new PaymentReceiptService(
            db,
            ledger.Object,
            docs.Object,
            clock,
            new PaymentReceiptGenerator(),
            http.Object,
            NullLogger<PaymentReceiptService>.Instance);

        var first = await service.GenerateAsync(paymentId, saveToVault: true);
        var second = await service.GenerateAsync(paymentId, saveToVault: true);

        first.DocumentId.Should().Be(existingDocId);
        second.DocumentId.Should().Be(existingDocId);
        docs.Verify(
            d => d.UploadAsync(
                It.IsAny<DocumentEntityType>(),
                It.IsAny<Guid>(),
                It.IsAny<DocumentCategory>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Stream>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
