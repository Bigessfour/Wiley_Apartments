namespace Wiley.Apartments.Domain;

public class Lease
{
    public Guid Id { get; set; }
    public Guid UnitId { get; set; }
    public Unit? Unit { get; set; }
    public Guid TenantId { get; set; }
    public Tenant? Tenant { get; set; }
    public DateTime StartUtc { get; set; }
    public DateTime EndUtc { get; set; }
    public decimal Rent { get; set; }
    public decimal Deposit { get; set; }
    public LeaseStatus Status { get; set; } = LeaseStatus.Draft;
    /// <summary>Template file name under DocumentRoot/templates (e.g. brookside-year-lease.docx).</summary>
    public string TemplateUsed { get; set; } = string.Empty;
    public bool IsDeleted { get; set; }
    public string? GeneratedDocxRelativePath { get; set; }
    public string? GeneratedPdfRelativePath { get; set; }
    /// <summary>Document vault id for the signed lease PDF (T3.3).</summary>
    public Guid? SignedDocumentId { get; set; }
}
