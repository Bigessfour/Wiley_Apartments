namespace Wiley.Apartments.Web.Configuration;

public class ClerkSuiteOptions
{
    public const string SectionName = "ClerkSuite";

    public string DatabaseProvider { get; set; } = "Sqlite";
    public string DocumentRoot { get; set; } = "/docs";
    public string PaymentPortalUrl { get; set; } =
        "https://www.townofwiley.gov/government/departments/finance/utility-billing";
    public bool LateFeesEnabled { get; set; }
    public decimal LateFeeAmount { get; set; }
    public int LateFeeGraceDays { get; set; }
    /// <summary>
    /// Soft residential unit cap (excludes facility CC). Default <c>16</c> per Edge Case 3 / FR-001.
    /// Set <c>ClerkSuite__MaxUnits=0</c> only as an admin override for unlimited creates.
    /// </summary>
    public int MaxUnits { get; set; } = 16;
}
