namespace Wiley.Apartments.Domain;

/// <summary>Metadata for a file on the NAS DocumentRoot share. Bytes are never stored in SQLite.</summary>
public class Document
{
    public Guid Id { get; set; }
    public DocumentEntityType EntityType { get; set; }
    public Guid EntityId { get; set; }
    /// <summary>Path relative to DocumentRoot (e.g. leases/3/signed/....pdf).</summary>
    public string FilePathOnNas { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/pdf";
    public DocumentCategory Category { get; set; } = DocumentCategory.Other;
    public string UploadedBy { get; set; } = string.Empty;
    public DateTime UploadedAtUtc { get; set; }
    public bool IsDeleted { get; set; }
}
