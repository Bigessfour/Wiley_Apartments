using Wiley.Apartments.Domain;

namespace Wiley.Apartments.Contracts;

public sealed record DocumentInfo(
    Guid Id,
    DocumentEntityType EntityType,
    Guid EntityId,
    string FilePathOnNas,
    string OriginalFileName,
    string ContentType,
    DocumentCategory Category,
    string UploadedBy,
    DateTime UploadedAtUtc);
