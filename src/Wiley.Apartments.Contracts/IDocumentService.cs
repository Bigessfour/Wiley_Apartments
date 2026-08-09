using Wiley.Apartments.Domain;

namespace Wiley.Apartments.Contracts;

public interface IDocumentService
{
    Task<IReadOnlyList<DocumentInfo>> ListForEntityAsync(
        DocumentEntityType entityType,
        Guid entityId,
        CancellationToken cancellationToken = default);

    Task<DocumentInfo?> GetByIdAsync(Guid documentId, CancellationToken cancellationToken = default);

    Task<DocumentInfo> UploadAsync(
        DocumentEntityType entityType,
        Guid entityId,
        DocumentCategory category,
        string originalFileName,
        string contentType,
        Stream content,
        string uploadedBy,
        string relativeDirectory,
        CancellationToken cancellationToken = default);

    Task<byte[]?> ReadBytesAsync(Guid documentId, CancellationToken cancellationToken = default);

    Task SoftDeleteAsync(Guid documentId, CancellationToken cancellationToken = default);
}
