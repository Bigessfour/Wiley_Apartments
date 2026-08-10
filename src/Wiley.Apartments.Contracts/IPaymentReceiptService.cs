namespace Wiley.Apartments.Contracts;

public interface IPaymentReceiptService
{
    /// <summary>Build a payment receipt PDF for a Payment ledger entry; optionally store in the document vault.</summary>
    Task<PaymentReceiptResult> GenerateAsync(
        Guid paymentEntryId,
        bool saveToVault = true,
        string uploadedBy = "clerk",
        CancellationToken cancellationToken = default);
}

public sealed record PaymentReceiptResult(
    Guid PaymentEntryId,
    string ReceiptNumber,
    byte[] PdfBytes,
    string FileName,
    Guid? DocumentId);
