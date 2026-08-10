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
    /// Soft residential unit cap. <c>0</c> (default) = unlimited.
    /// Set a positive value via <c>ClerkSuite__MaxUnits</c> only if you want a hard stop.
    /// </summary>
    public int MaxUnits { get; set; }
}
