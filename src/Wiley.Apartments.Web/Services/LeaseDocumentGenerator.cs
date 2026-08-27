using Syncfusion.DocIO;
using Syncfusion.DocIO.DLS;
using Syncfusion.DocIORenderer;
using Syncfusion.Drawing;
using Syncfusion.Pdf;
using Syncfusion.Pdf.Graphics;
using Syncfusion.Pdf.Interactive;
using Syncfusion.Pdf.Parsing;
using Wiley.Apartments.Contracts;

namespace Wiley.Apartments.Web.Services;

/// <summary>
/// Lease document generation for Brookside templates.
/// Preferred path: fillable AcroForm PDF (named fields). Fallback: DocIO underscore replace on DOCX → PDF.
/// </summary>
public sealed class LeaseDocumentGenerator
{
    private static readonly (string Marker, string FieldName, string FindBlank, string ReplaceWithMarker)[] FieldMap =
    [
        ("@@AgreementDay@@", "AgreementDay", "On this_______ day of", "On this @@AgreementDay@@ day of"),
        ("@@AgreementMonthYear@@", "AgreementMonthYear", "day of _____________, ________ the Housing Authority",
            "day of @@AgreementMonthYear@@ the Housing Authority"),
        ("@@PremisesAddress@@", "PremisesAddress", "apartment at ____________________",
            "apartment at @@PremisesAddress@@"),
        ("@@ResidentName@@", "ResidentName", "to _________________________________referred to as resident",
            "to @@ResidentName@@ referred to as resident"),
        ("@@HouseholdMembers@@", "HouseholdMembers", "Household members are:_____________________________________.",
            "Household members are:@@HouseholdMembers@@."),
        ("@@PostOfficeBox@@", "PostOfficeBox", "POST OFFICE BOX____________________________________________",
            "POST OFFICE BOX @@PostOfficeBox@@"),
        ("@@PhoneNumber@@", "PhoneNumber", "PHONE NUMBER_____________________________________________",
            "PHONE NUMBER @@PhoneNumber@@"),
        ("@@ApartmentNumber@@", "ApartmentNumber", "APARTMENT NUMBER________________________________________",
            "APARTMENT NUMBER @@ApartmentNumber@@"),
        ("@@LeaseTerm@@", "LeaseTerm", "lease shall begin on ___________________ and end on_________________.",
            "lease shall begin on @@LeaseTerm@@."),
        ("@@MonthlyRent@@", "MonthlyRent", "rent for this initial period is_____",
            "rent for this initial period is @@MonthlyRent@@"),
        ("@@RentStart@@", "RentStart", "each month beginning ___________________________.",
            "each month beginning @@RentStart@@."),
        ("@@SecurityDeposit@@", "SecurityDeposit", "Resident has deposited $___________ with the owner",
            "Resident has deposited $@@SecurityDeposit@@ with the owner")
    ];

    /// <summary>Fill a fillable PDF template (AcroForm) and return PDF bytes.</summary>
    public byte[] FillPdf(Stream fillablePdfTemplate, LeaseMergeData data, bool flatten = true)
    {
        using var loaded = new PdfLoadedDocument(fillablePdfTemplate);
        if (loaded.Form is null || loaded.Form.Fields.Count == 0)
        {
            throw new InvalidOperationException("PDF template has no AcroForm fields.");
        }

        var values = ToFieldValues(data);
        foreach (PdfField field in loaded.Form.Fields)
        {
            if (field is PdfLoadedTextBoxField textBox && values.TryGetValue(field.Name, out var value))
            {
                textBox.Text = value;
            }
            else if (field is PdfTextBoxField created && values.TryGetValue(field.Name, out value))
            {
                created.Text = value;
            }
        }

        if (flatten)
        {
            loaded.Form.Flatten = true;
        }

        using var output = new MemoryStream();
        loaded.Save(output);
        var filled = output.ToArray();
        return AppendCustomClausesIfNeeded(filled, data.CustomClauses);
    }

    /// <summary>
    /// Build a fillable AcroForm PDF from a Brookside blank DOCX (underscore blanks → named fields).
    /// </summary>
    public byte[] CreateFillablePdfFromDocx(Stream templateDocx)
    {
        using var document = new WordDocument(templateDocx, FormatType.Docx);
        foreach (var (_, _, find, replace) in FieldMap)
        {
            document.Replace(find, replace, false, true);
        }

        using var pdfRenderer = new DocIORenderer();
        using var pdfDocument = pdfRenderer.ConvertToPDF(document);
        using var pdfStream = new MemoryStream();
        pdfDocument.Save(pdfStream);
        pdfStream.Position = 0;

        using var loaded = new PdfLoadedDocument(pdfStream);
        foreach (var (marker, fieldName, _, _) in FieldMap)
        {
            if (!loaded.FindText(marker, out Dictionary<int, List<RectangleF>>? matches) || matches is null)
            {
                continue;
            }

            foreach (var (pageIndex, rects) in matches)
            {
                var rect = rects.FirstOrDefault(r => r.Width > 1 && r.Height > 1);
                if (rect.Width <= 1)
                {
                    continue;
                }

                var page = loaded.Pages[pageIndex];
                // Cover marker text so the form value is what clerks see.
                page.Graphics.DrawRectangle(PdfBrushes.White, rect);
                var field = new PdfTextBoxField(page, fieldName)
                {
                    Bounds = new RectangleF(rect.X, rect.Y, Math.Max(rect.Width, 80f), rect.Height + 2f),
                    Text = string.Empty,
                    BorderWidth = 0,
                    BackColor = Color.White
                };
                loaded.Form.Fields.Add(field);
            }
        }

        if (loaded.Form.Fields.Count == 0)
        {
            throw new InvalidOperationException(
                "Could not place AcroForm fields on Brookside PDF (marker FindText failed).");
        }

        using var output = new MemoryStream();
        loaded.Save(output);
        return output.ToArray();
    }

    /// <summary>Legacy DOCX underscore fill + PDF export (fallback when no fillable PDF).</summary>
    public (byte[] Docx, byte[] Pdf) GenerateFromDocx(Stream templateDocx, LeaseMergeData data)
    {
        using var document = new WordDocument(templateDocx, FormatType.Docx);
        ApplyDocxReplacements(document, data);

        using var docxStream = new MemoryStream();
        document.Save(docxStream, FormatType.Docx);
        var docxBytes = docxStream.ToArray();

        using var pdfRenderer = new DocIORenderer();
        using var pdfDocument = pdfRenderer.ConvertToPDF(document);
        using var pdfStream = new MemoryStream();
        pdfDocument.Save(pdfStream);
        var pdfBytes = OverlayMergeValues(pdfStream.ToArray(), data);
        pdfBytes = RewriteSectionTwo(pdfBytes, data);
        pdfBytes = PrependLetterheadPage(pdfBytes, data);
        pdfBytes = AppendCustomClausesIfNeeded(pdfBytes, data.CustomClauses);
        return (docxBytes, pdfBytes);
    }

    /// <summary>
    /// Clerk-facing PDF: DOCX uses underscore/marker text replace (same idea as CC agreements).
    /// Fillable PDF templates are filled then leftover @@markers@@ are painted over.
    /// </summary>
    public (byte[]? Docx, byte[] Pdf) Generate(Stream template, string templateFileName, LeaseMergeData data)
    {
        if (templateFileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            var pdf = FillPdf(template, data);
            pdf = OverlayMergeValues(pdf, data);
            pdf = RewriteSectionTwo(pdf, data);
            pdf = PrependLetterheadPage(pdf, data);
            return (null, pdf);
        }

        using var forDocx = new MemoryStream();
        template.CopyTo(forDocx);
        forDocx.Position = 0;
        return GenerateFromDocx(forDocx, data);
    }

    /// <summary>True when the PDF still shows unmerged @@Field@@ tokens.</summary>
    public static bool ContainsMergeMarkers(byte[] pdf)
    {
        using var loaded = new PdfLoadedDocument(pdf);
        for (var i = 0; i < loaded.Pages.Count; i++)
        {
            if (loaded.Pages[i].ExtractText().Contains("@@", StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Cover leftover @@Marker@@ text and draw merge values. AcroForm FindText often misses
    /// markers after DOCX→PDF, which is what left unreadable leases on Regenerate.
    /// </summary>
    public byte[] OverlayMergeValues(byte[] pdf, LeaseMergeData data)
    {
        var values = ToFieldValues(data);
        using var input = new MemoryStream(pdf);
        using var loaded = new PdfLoadedDocument(input);
        var font = new PdfStandardFont(PdfFontFamily.Helvetica, 9);

        foreach (var (marker, fieldName, _, _) in FieldMap)
        {
            if (!values.TryGetValue(fieldName, out var value) || string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            if (!loaded.FindText(marker, out Dictionary<int, List<RectangleF>>? matches) || matches is null)
            {
                continue;
            }

            foreach (var (pageIndex, rects) in matches)
            {
                var page = loaded.Pages[pageIndex];
                foreach (var rect in rects.Where(r => r.Width > 1 && r.Height > 1))
                {
                    page.Graphics.DrawRectangle(PdfBrushes.White, rect);
                    var bounds = new RectangleF(rect.X, rect.Y, Math.Max(rect.Width, 80f), rect.Height + 2f);
                    page.Graphics.DrawString(value, font, PdfBrushes.Black, bounds);
                }
            }
        }

        using var output = new MemoryStream();
        loaded.Save(output);
        return output.ToArray();
    }

    /// <summary>Append an Additional Clauses page when the clerk provided custom text (FR-009).</summary>
    public byte[] AppendCustomClausesIfNeeded(byte[] pdfBytes, string? customClauses)
    {
        if (string.IsNullOrWhiteSpace(customClauses))
        {
            return pdfBytes;
        }

        using var input = new MemoryStream(pdfBytes);
        using var loaded = new PdfLoadedDocument(input);
        var page = loaded.Pages.Add();
        var graphics = page.Graphics;
        var titleFont = new PdfStandardFont(PdfFontFamily.Helvetica, 14, PdfFontStyle.Bold);
        var bodyFont = new PdfStandardFont(PdfFontFamily.Helvetica, 11);
        graphics.DrawString("Additional Clauses (Addendum)", titleFont, PdfBrushes.Black, new PointF(40, 40));
        graphics.DrawString(
            "The following clauses are incorporated into and made part of this lease:",
            bodyFont,
            PdfBrushes.Black,
            new PointF(40, 70));
        var pageSize = page.Size;
        var bounds = new RectangleF(40, 100, pageSize.Width - 80, pageSize.Height - 140);
        graphics.DrawString(customClauses.Trim(), bodyFont, PdfBrushes.Black, bounds);
        using var output = new MemoryStream();
        loaded.Save(output);
        return output.ToArray();
    }

    private static Dictionary<string, string> ToFieldValues(LeaseMergeData data)
    {
        var agreement = data.AgreementDate;
        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["AgreementDay"] = agreement.Day.ToString(),
            ["AgreementMonthYear"] = $"{agreement:MMMM}, {agreement:yyyy}",
            ["PremisesAddress"] = data.PremisesAddress.Trim(),
            ["ResidentName"] = data.ResidentName.Trim(),
            ["HouseholdMembers"] = data.HouseholdMembers.Trim(),
            ["PostOfficeBox"] = string.IsNullOrWhiteSpace(data.PostOfficeBox) ? "—" : data.PostOfficeBox.Trim(),
            ["PhoneNumber"] = FormatUsPhone(data.PhoneNumber),
            ["ApartmentNumber"] = data.ApartmentNumber.Trim(),
            ["LeaseTerm"] = $"{data.LeaseStart:MMMM d, yyyy} and end on {data.LeaseEnd:MMMM d, yyyy}",
            ["MonthlyRent"] = data.MonthlyRent.ToString("0.00"),
            ["RentStart"] = data.RentStart.ToString("MMMM d, yyyy"),
            ["SecurityDeposit"] = data.SecurityDeposit.ToString("0.00")
        };
    }

    private static void ApplyDocxReplacements(WordDocument document, LeaseMergeData data)
    {
        var values = ToFieldValues(data);
        Replace(document, "On this_______ day of", $"On this {values["AgreementDay"]} day of");
        Replace(document, "day of _____________, ________ the Housing Authority",
            $"day of {values["AgreementMonthYear"]} the Housing Authority");
        Replace(document, "apartment at ____________________",
            $"apartment at {values["PremisesAddress"]}");
        Replace(document, "to _________________________________referred to as resident",
            $"to {values["ResidentName"]} referred to as resident");
        Replace(document, "Household members are:_____________________________________.",
            $"Household members are: {values["HouseholdMembers"]}.");
        Replace(document, "POST OFFICE BOX____________________________________________",
            $"POST OFFICE BOX: {values["PostOfficeBox"]}");
        Replace(document, "PHONE NUMBER_____________________________________________",
            $"PHONE NUMBER: {values["PhoneNumber"]}");
        Replace(document, "APARTMENT NUMBER________________________________________",
            $"APARTMENT NUMBER: {values["ApartmentNumber"]}");
        Replace(document, "lease shall begin on ___________________ and end on_________________.",
            $"lease shall begin on {values["LeaseTerm"]}.");
        Replace(document, "rent for this initial period is_____",
            $"rent for this initial period is {values["MonthlyRent"]}");
        Replace(document, "each month beginning ___________________________.",
            $"each month beginning {values["RentStart"]}.");
        Replace(document, "Resident has deposited $___________ with the owner",
            $"Resident has deposited ${values["SecurityDeposit"]} with the owner");

        foreach (var (marker, fieldName, _, _) in FieldMap)
        {
            Replace(document, marker, values[fieldName]);
        }
    }

    private static void Replace(WordDocument document, string find, string replace) =>
        document.Replace(find, replace, false, true);

    internal const string TownName = "Town of Wiley";
    internal const string TownStreet = "304 Main Street";
    internal const string TownCityLine = "Wiley, CO 81092";
    internal const string TownPhone = "(719) 829-4974";

    /// <summary>Formats a 10-digit US number as (719) 555-0100; otherwise returns trimmed input.</summary>
    internal static string FormatUsPhone(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
        {
            return string.Empty;
        }

        var trimmed = phone.Trim();
        var digits = new string(trimmed.Where(char.IsDigit).ToArray());
        if (digits.Length == 11 && digits[0] == '1')
        {
            digits = digits[1..];
        }

        return digits.Length == 10
            ? $"({digits[..3]}) {digits[3..6]}-{digits[6..]}"
            : trimmed;
    }

    /// <summary>
    /// Cover cramped section 2 underscore fill with labeled occupant lines (household, mail, phone, unit).
    /// </summary>
    internal static byte[] RewriteSectionTwo(byte[] pdf, LeaseMergeData data)
    {
        var values = ToFieldValues(data);
        using var input = new MemoryStream(pdf);
        using var loaded = new PdfLoadedDocument(input);
        if (!loaded.FindText("Household members are", out Dictionary<int, List<RectangleF>>? matches)
            || matches is null
            || matches.Count == 0)
        {
            using var passthrough = new MemoryStream();
            loaded.Save(passthrough);
            return passthrough.ToArray();
        }

        var (pageIndex, rects) = matches.First();
        var start = rects.Where(r => r.Width > 1).OrderBy(r => r.Y).FirstOrDefault();
        if (start.Width <= 1)
        {
            using var passthrough = new MemoryStream();
            loaded.Save(passthrough);
            return passthrough.ToArray();
        }

        var page = loaded.Pages[pageIndex];
        var pageSize = page.Size;
        var coverTop = Math.Max(start.Y - 2f, 0f);
        var coverHeight = 72f;
        if (loaded.FindText("Initial period", out Dictionary<int, List<RectangleF>>? endMatches)
            && endMatches is not null
            && endMatches.TryGetValue(pageIndex, out var endRects))
        {
            var end = endRects.Where(r => r.Width > 1 && r.Y > start.Y).OrderBy(r => r.Y).FirstOrDefault();
            if (end.Width > 1)
            {
                coverHeight = Math.Max(36f, end.Y - coverTop - 4f);
            }
        }

        var cover = new RectangleF(36f, coverTop, pageSize.Width - 72f, coverHeight);
        page.Graphics.DrawRectangle(PdfBrushes.White, cover);

        var font = new PdfStandardFont(PdfFontFamily.Helvetica, 10);
        var bold = new PdfStandardFont(PdfFontFamily.Helvetica, 10, PdfFontStyle.Bold);
        var lines = new[]
        {
            ("2. Household members:", values["HouseholdMembers"]),
            ("    Post office box:", values["PostOfficeBox"]),
            ("    Phone number:", values["PhoneNumber"]),
            ("    Apartment number:", values["ApartmentNumber"])
        };

        float y = coverTop + 2f;
        foreach (var (label, value) in lines)
        {
            page.Graphics.DrawString(label, bold, PdfBrushes.Black, new PointF(40f, y));
            var valueBounds = new RectangleF(190f, y, pageSize.Width - 230f, 16f);
            page.Graphics.DrawString(value, font, PdfBrushes.Black, valueBounds);
            y += 14f;
        }

        using var output = new MemoryStream();
        loaded.Save(output);
        return output.ToArray();
    }

    /// <summary>Town letterhead as page 1: name, 304 Main Street, Wiley CO 81092, hall phone.</summary>
    internal static byte[] PrependLetterheadPage(byte[] pdf, LeaseMergeData data)
    {
        using var coverDoc = new PdfDocument();
        coverDoc.PageSettings.Size = PdfPageSize.Letter;
        var coverPage = coverDoc.Pages.Add();
        DrawLetterhead(coverPage, data);
        using var coverStream = new MemoryStream();
        coverDoc.Save(coverStream);
        coverStream.Position = 0;

        using var coverLoaded = new PdfLoadedDocument(coverStream);
        using var bodyLoaded = new PdfLoadedDocument(new MemoryStream(pdf));
        using var merged = new PdfDocument();
        merged.ImportPage(coverLoaded, 0);
        for (var i = 0; i < bodyLoaded.Pages.Count; i++)
        {
            merged.ImportPage(bodyLoaded, i);
        }

        using var output = new MemoryStream();
        merged.Save(output);
        return output.ToArray();
    }

    private static void DrawLetterhead(PdfPageBase page, LeaseMergeData data)
    {
        var g = page.Graphics;
        var pageWidth = page.Size.Width;
        var title = new PdfStandardFont(PdfFontFamily.Helvetica, 18, PdfFontStyle.Bold);
        var sub = new PdfStandardFont(PdfFontFamily.Helvetica, 12, PdfFontStyle.Bold);
        var body = new PdfStandardFont(PdfFontFamily.Helvetica, 11);
        float y = 72f;

        void Line(string text, PdfFont font, float gap = 6f)
        {
            g.DrawString(text, font, PdfBrushes.Black, new RectangleF(48f, y, pageWidth - 96f, 24f));
            y += font.Size + gap;
        }

        Line(TownName, title, 10f);
        Line("Wiley Housing Authority", sub, 8f);
        Line("Brookside Community Living — Residential Lease", sub, 16f);
        Line(TownStreet, body);
        Line(TownCityLine, body);
        Line($"Phone: {TownPhone}", body, 20f);
        g.DrawLine(new PdfPen(Color.FromArgb(255, 31, 107, 92), 1.25f), 48f, y, pageWidth - 48f, y);
        y += 18f;
        Line($"Resident: {data.ResidentName.Trim()}", body);
        Line($"Household: {data.HouseholdMembers.Trim()}", body);
        Line($"Apartment: {data.ApartmentNumber.Trim()}", body);
        Line($"Term: {data.LeaseStart:MMMM d, yyyy} through {data.LeaseEnd:MMMM d, yyyy}", body);
        Line($"Phone: {FormatUsPhone(data.PhoneNumber)}", body, 22f);
        Line("The following pages are the residential lease agreement.", body, 8f);
        Line("Keep this letterhead page with the signed lease.", body);
    }
}
