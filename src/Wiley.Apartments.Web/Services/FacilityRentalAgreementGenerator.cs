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
        var page = document.Pages.Add();
        var g = page.Graphics;
        var titleFont = new PdfStandardFont(PdfFontFamily.Helvetica, 16, PdfFontStyle.Bold);
        var body = new PdfStandardFont(PdfFontFamily.Helvetica, 10);
        var bold = new PdfStandardFont(PdfFontFamily.Helvetica, 10, PdfFontStyle.Bold);
        float y = 40;

        void Line(string label, string value)
        {
            g.DrawString(label, bold, PdfBrushes.Black, new PointF(40, y));
            g.DrawString(value, body, PdfBrushes.Black, new PointF(180, y));
            y += 18;
        }

        g.DrawString("Town of Wiley — Community Center Rental Agreement", titleFont, PdfBrushes.Black, new PointF(40, y));
        y += 28;
        g.DrawString("This agreement is for facility hall hire (not a residential lease).", body, PdfBrushes.Black, new PointF(40, y));
        y += 24;

        Line("Renter:", data.RenterName);
        Line("Organization:", data.Organization);
        Line("Address:", data.MailingAddress);
        Line("Phone:", data.Phone);
        Line("Email:", data.Email);
        Line("Event start:", data.StartLocal);
        Line("Event end:", data.EndLocal);
        Line("Rental fee:", data.RentalFee);
        Line("Damage deposit:", data.DepositAmount);
        y += 10;

        g.DrawString("Terms (summary)", bold, PdfBrushes.Black, new PointF(40, y));
        y += 16;
        var terms =
            "1. Booking is confirmed only when this agreement is issued and deposit/fee terms are met.\n" +
            "2. Renter is responsible for cleanup and for any damage beyond normal wear.\n" +
            "3. Deposit may be retained in whole or part for damages documented on inspection.\n" +
            "4. Cancellation and refunds follow town clerk policy.\n" +
            "5. No alcohol without required permits. Occupancy and fire rules apply.\n" +
            (string.IsNullOrWhiteSpace(data.CustomNotes) ? "" : $"6. Additional: {data.CustomNotes}\n");

        var format = new PdfStringFormat { LineSpacing = 14f };
        var bounds = new RectangleF(40, y, page.GetClientSize().Width - 80, 180);
        g.DrawString(terms, body, PdfBrushes.Black, bounds, format);
        y = bounds.Bottom + 24;

        g.DrawString($"Generated: {data.GeneratedLocal}", body, PdfBrushes.Black, new PointF(40, y));
        y += 36;
        g.DrawString("Renter signature: ___________________________  Date: __________", body, PdfBrushes.Black, new PointF(40, y));
        y += 28;
        g.DrawString("Clerk signature:  ___________________________  Date: __________", body, PdfBrushes.Black, new PointF(40, y));

        using var ms = new MemoryStream();
        document.Save(ms);
        return ms.ToArray();
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
    string RentalFee,
    string DepositAmount,
    string? CustomNotes,
    string GeneratedLocal);
