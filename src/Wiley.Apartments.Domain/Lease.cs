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
    /// <summary>Optional clerk addendum text appended as a PDF page (FR-009 custom clauses).</summary>
    public string? CustomClauses { get; set; }
    /// <summary>When set, this lease was created by renewing <see cref="PriorLeaseId"/>.</summary>
    public Guid? PriorLeaseId { get; set; }
    /// <summary>When set, this lease was renewed into <see cref="SuccessorLeaseId"/>.</summary>
    public Guid? SuccessorLeaseId { get; set; }
    /// <summary>Clerk note for terminate / amend / renew actions.</summary>
    public string? LifecycleNote { get; set; }
    /// <summary>Optimistic concurrency token (SQLite-friendly Guid).</summary>
    public Guid RowVersion { get; set; } = Guid.NewGuid();
}
