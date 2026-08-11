namespace Wiley.Apartments.Contracts;

/// <summary>Values filled into Brookside blank lease DOCX templates.</summary>
public sealed class LeaseMergeData
{
    public DateTime AgreementDate { get; set; }
    public string PremisesAddress { get; set; } = string.Empty;
    public string ResidentName { get; set; } = string.Empty;
    public string HouseholdMembers { get; set; } = string.Empty;
    public string PostOfficeBox { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string ApartmentNumber { get; set; } = string.Empty;
    public DateTime LeaseStart { get; set; }
    public DateTime LeaseEnd { get; set; }
    public decimal MonthlyRent { get; set; }
    public DateTime RentStart { get; set; }
    public decimal SecurityDeposit { get; set; }
    /// <summary>Optional addendum; when non-empty, generator appends an Additional Clauses PDF page.</summary>
    public string? CustomClauses { get; set; }
}
