using System.Text;

namespace Wiley.Apartments.Web.Services;

/// <summary>
/// Clerk-facing lease file names: LastName-FirstName-Unit{n}-yyyy-MM-dd (no GUID).
/// </summary>
public static class LeaseDocumentFileName
{
    public static string BuildBaseName(
        string? lastName,
        string? firstName,
        string? unitNumber,
        DateTime startLocalDate)
    {
        var last = SanitizeSegment(lastName);
        var first = SanitizeSegment(firstName);
        var unit = SanitizeSegment(unitNumber);
        var unitPart = unit.StartsWith("Unit", StringComparison.OrdinalIgnoreCase)
            ? unit
            : "Unit" + unit;
        return $"{last}-{first}-{unitPart}-{startLocalDate:yyyy-MM-dd}";
    }

    public static string PdfFileName(
        string? lastName,
        string? firstName,
        string? unitNumber,
        DateTime startLocalDate) =>
        BuildBaseName(lastName, firstName, unitNumber, startLocalDate) + ".pdf";

    private static string SanitizeSegment(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "Unknown";
        }

        var sb = new StringBuilder(value.Length);
        foreach (var c in value.Trim())
        {
            if (char.IsLetterOrDigit(c))
            {
                sb.Append(c);
            }
        }

        return sb.Length == 0 ? "Unknown" : sb.ToString();
    }
}
