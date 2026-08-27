using Syncfusion.Drawing;
using Syncfusion.Pdf;
using Syncfusion.Pdf.Graphics;

namespace Wiley.Apartments.Web.Services;

/// <summary>Draws a Community Center rental agreement PDF (template file optional later).</summary>
public sealed class FacilityRentalAgreementGenerator
{
    public byte[] Generate(FacilityRentalAgreementData data)
    {
        using var document = new PdfDocument();
        document.Compression = PdfCompressionLevel.None;
        var page = document.Pages.Add();
        var g = page.Graphics;
        var titleFont = new PdfStandardFont(PdfFontFamily.Helvetica, 16, PdfFontStyle.Bold);
        var sectionFont = new PdfStandardFont(PdfFontFamily.Helvetica, 12, PdfFontStyle.Bold);
        var body = new PdfStandardFont(PdfFontFamily.Helvetica, 10);
        var bold = new PdfStandardFont(PdfFontFamily.Helvetica, 10, PdfFontStyle.Bold);
        var pageWidth = page.GetClientSize().Width;
        float y = 36;

        void Text(string value, PdfFont font, float x, float width)
        {
            var element = new PdfTextElement(WinAnsi(value), font, PdfBrushes.Black);
            var layout = element.Draw(page, new RectangleF(x, y, width, 400));
            y = layout.Bounds.Bottom + 4;
        }

        void Line(string label, string value)
        {
            g.DrawString(WinAnsi(label), bold, PdfBrushes.Black, new PointF(40, y));
            var valueElement = new PdfTextElement(WinAnsi(value), body, PdfBrushes.Black);
            var layout = valueElement.Draw(page, new RectangleF(180, y, pageWidth - 220, 60));
            y = Math.Max(y + 16, layout.Bounds.Bottom + 4);
        }

        Text("Town of Wiley", titleFont, 40, pageWidth - 80);
        Text("Community Center Rental Agreement", sectionFont, 40, pageWidth - 80);
        Text(
            "This agreement is for facility hall hire (not a residential apartment lease).",
            body,
            40,
            pageWidth - 80);
        y += 8;

        Text("Renter", sectionFont, 40, pageWidth - 80);
        Line("Renter:", data.RenterName);
        Line("Organization:", data.Organization);
        Line("Address:", data.MailingAddress);
        Line("Phone:", data.Phone);
        Line("Email:", data.Email);
        y += 8;

        Text("Event", sectionFont, 40, pageWidth - 80);
        Line("Event start:", data.StartLocal);
        Line("Event end:", data.EndLocal);
        Line("Space:", data.Space);
        Line("Equipment:", data.Equipment);
        Line("Rental fee:", data.RentalFee);
        Line("Damage deposit:", data.DepositAmount);
        y += 8;

        Text("Terms", sectionFont, 40, pageWidth - 80);
        var terms =
            "1. Booking is confirmed only when this agreement is issued and deposit/fee terms are met.\n" +
            "2. Renter is responsible for cleanup and for any damage beyond normal wear.\n" +
            "3. Deposit may be retained in whole or part for damages documented on inspection.\n" +
            "4. Cancellation and refunds follow town clerk policy.\n" +
            "5. No alcohol without required permits. Occupancy and fire rules apply.";
        if (!string.IsNullOrWhiteSpace(data.CustomNotes))
        {
            terms += "\n6. Additional: " + data.CustomNotes.Trim();
        }

        Text(terms, body, 40, pageWidth - 80);
        y += 8;
        Text("Generated: " + data.GeneratedLocal, body, 40, pageWidth - 80);
        y += 16;
        Text("Renter signature: ___________________________  Date: __________", body, 40, pageWidth - 80);
        Text("Clerk signature:  ___________________________  Date: __________", body, 40, pageWidth - 80);

        using var ms = new MemoryStream();
        document.Save(ms);
        var bytes = ms.ToArray();
        if (bytes.Length < 5 || bytes[0] != (byte)'%' || bytes[1] != (byte)'P'
            || bytes[2] != (byte)'D' || bytes[3] != (byte)'F')
        {
            throw new InvalidOperationException("Community Center agreement PDF did not generate as a valid PDF.");
        }

        return bytes;
    }

    /// <summary>PdfStandardFont Helvetica is WinAnsi; strip characters that render blank or missing.</summary>
    private static string WinAnsi(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return "-";
        }

        return value
            .Replace('\u2014', '-')
            .Replace('\u2013', '-')
            .Replace('\u2018', '\'')
            .Replace('\u2019', '\'')
            .Replace('\u201C', '"')
            .Replace('\u201D', '"')
            .Replace('\u00A0', ' ')
            .Replace("\u2026", "...");
    }
}

public sealed record FacilityRentalAgreementData(
    string RenterName,
    string Organization,
    string MailingAddress,
    string Phone,
    string Email,
    string StartLocal,
    string EndLocal,
    string Space,
    string Equipment,
    string RentalFee,
    string DepositAmount,
    string? CustomNotes,
    string GeneratedLocal);
