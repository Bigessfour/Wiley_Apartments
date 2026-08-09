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
}
