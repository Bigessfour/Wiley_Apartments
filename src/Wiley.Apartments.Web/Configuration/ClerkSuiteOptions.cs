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

    /// <summary>
    /// When true, a clerk who can reach this host is signed in as the office clerk.
    /// Town Hub can open the app directly. Leave false on any host that is not the office tailnet.
    /// </summary>
    public bool OfficeNetworkSignIn { get; set; }

    /// <summary>
    /// Optional email for <see cref="OfficeNetworkSignIn"/>. When empty, the first Identity user is used.
    /// </summary>
    public string? OfficeClerkEmail { get; set; }

    /// <summary>
    /// ClerkSuite hosted UI host, without a scheme. Empty skips the silent session probe.
    /// </summary>
    public string? CognitoDomain { get; set; }

    /// <summary>
    /// Public app client on the Town of Wiley staff pool. No client secret.
    /// </summary>
    public string CognitoClientId { get; set; } = Infrastructure.CognitoStaffAuth.DefaultClientId;

    /// <summary>Region of <see cref="CognitoUserPoolId"/>. Used to verify the ID token issuer.</summary>
    public string CognitoRegion { get; set; } = Infrastructure.CognitoStaffAuth.DefaultRegion;

    /// <summary>Staff pool whose signing keys must match the ID token.</summary>
    public string CognitoUserPoolId { get; set; } = Infrastructure.CognitoStaffAuth.DefaultUserPoolId;

    /// <summary>
    /// Shared secret for the operating-cost export script. Empty means only a signed-in clerk can export.
    /// Set <c>ClerkSuite__ExportKey</c> in the NAS env file. Never commit the value.
    /// </summary>
    public string? ExportKey { get; set; }
}
