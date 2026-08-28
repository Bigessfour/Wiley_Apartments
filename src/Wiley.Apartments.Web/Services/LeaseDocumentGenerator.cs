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
/// Lease document generation for Brookside. Clerk-facing PDF/DOCX are built as a
/// professional Times Roman legal layout (not the 7pt blank DOCX with painted overlays).
/// Fillable AcroForm bootstrap from the blank DOCX is retained for template inventory.
/// </summary>
public sealed class LeaseDocumentGenerator
{
    private const string BodyFont = "Times New Roman";
    private const float BodySize = 11f;
    private const float TitleSize = 16f;
    private const float SubtitleSize = 12f;
    private const float SmallSize = 9.5f;

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

    /// <summary>Clerk-facing lease: professional Times Roman layout from merge data.</summary>
    public (byte[] Docx, byte[] Pdf) GenerateFromDocx(Stream templateDocx, LeaseMergeData data)
    {
        var monthToMonth = IsMonthToMonthTemplate(templateDocx);
        return GenerateProfessional(data, monthToMonth);
    }

    /// <summary>
    /// Clerk-facing PDF/DOCX with professional typography. Template stream is used only to
    /// detect year vs month-to-month when the file name is unavailable.
    /// </summary>
    public (byte[]? Docx, byte[] Pdf) Generate(Stream template, string templateFileName, LeaseMergeData data)
    {
        var monthToMonth = templateFileName.Contains("month", StringComparison.OrdinalIgnoreCase);
        var built = GenerateProfessional(data, monthToMonth);
        return (built.Docx, built.Pdf);
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

    /// <summary>Cover leftover @@Marker@@ text (legacy helper retained for tests/tools).</summary>
    public byte[] OverlayMergeValues(byte[] pdf, LeaseMergeData data)
    {
        var values = ToFieldValues(data);
        using var input = new MemoryStream(pdf);
        using var loaded = new PdfLoadedDocument(input);
        var font = new PdfStandardFont(PdfFontFamily.TimesRoman, 10);

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
        var titleFont = new PdfStandardFont(PdfFontFamily.TimesRoman, 14, PdfFontStyle.Bold);
        var bodyFont = new PdfStandardFont(PdfFontFamily.TimesRoman, 11);
        graphics.DrawString("Additional Clauses (Addendum)", titleFont, PdfBrushes.Black, new PointF(54, 54));
        graphics.DrawString(
            "The following clauses are incorporated into and made part of this lease:",
            bodyFont,
            PdfBrushes.Black,
            new PointF(54, 80));
        var pageSize = page.Size;
        var bounds = new RectangleF(54, 108, pageSize.Width - 108, pageSize.Height - 160);
        graphics.DrawString(customClauses.Trim(), bodyFont, PdfBrushes.Black, bounds);
        using var output = new MemoryStream();
        loaded.Save(output);
        return output.ToArray();
    }

    internal (byte[] Docx, byte[] Pdf) GenerateProfessional(LeaseMergeData data, bool monthToMonth)
    {
        using var document = BuildProfessionalDocument(data, monthToMonth);

        using var docxStream = new MemoryStream();
        document.Save(docxStream, FormatType.Docx);
        var docxBytes = docxStream.ToArray();

        using var pdfRenderer = new DocIORenderer();
        using var pdfDocument = pdfRenderer.ConvertToPDF(document);
        using var pdfStream = new MemoryStream();
        pdfDocument.Save(pdfStream);
        var pdfBytes = pdfStream.ToArray();
        pdfBytes = AppendCustomClausesIfNeeded(pdfBytes, data.CustomClauses);
        return (docxBytes, pdfBytes);
    }

    internal static WordDocument BuildProfessionalDocument(LeaseMergeData data, bool monthToMonth)
    {
        var values = ToFieldValues(data);
        var document = new WordDocument();
        var section = document.AddSection();
        section.PageSetup.PageSize = PageSize.Letter;
        section.PageSetup.Margins.Top = 72f;
        section.PageSetup.Margins.Bottom = 72f;
        section.PageSetup.Margins.Left = 72f;
        section.PageSetup.Margins.Right = 72f;

        var footer = section.HeadersFooters.Footer.AddParagraph();
        footer.ParagraphFormat.HorizontalAlignment = HorizontalAlignment.Center;
        AppendText(footer, "Brookside Community Living · Town of Wiley Housing Authority · Page ", SmallSize, italic: true);
        var pageField = footer.AppendField("Page", FieldType.FieldPage);
        pageField.CharacterFormat.FontName = BodyFont;
        pageField.CharacterFormat.FontSize = SmallSize;
        pageField.CharacterFormat.Italic = true;
        AppendText(footer, " of ", SmallSize, italic: true);
        var numPages = footer.AppendField("NumPages", FieldType.FieldNumPages);
        numPages.CharacterFormat.FontName = BodyFont;
        numPages.CharacterFormat.FontSize = SmallSize;
        numPages.CharacterFormat.Italic = true;

        AddCentered(section, TownName, TitleSize, bold: true, after: 4f);
        AddCentered(section, "Wiley Housing Authority", SubtitleSize, bold: true, after: 2f);
        AddCentered(section, "Brookside Community Living", SubtitleSize, bold: false, after: 10f);
        AddCentered(section, TownStreet, BodySize, after: 0f);
        AddCentered(section, TownCityLine, BodySize, after: 0f);
        AddCentered(section, $"Phone {TownPhone}", BodySize, after: 10f);

        AddRule(section);

        AddCentered(section, "RESIDENTIAL LEASE AGREEMENT", SubtitleSize, bold: true, after: 12f);

        AddKeyValue(section, "Resident", values["ResidentName"]);
        AddKeyValue(section, "Household", values["HouseholdMembers"]);
        AddKeyValue(section, "Apartment", values["ApartmentNumber"]);
        AddKeyValue(section, "Premises", values["PremisesAddress"]);
        AddKeyValue(section, "Term", $"{data.LeaseStart:MMMM d, yyyy} through {data.LeaseEnd:MMMM d, yyyy}");
        AddKeyValue(section, "Monthly rent", $"${values["MonthlyRent"]}");
        AddKeyValue(section, "Security deposit", $"${values["SecurityDeposit"]}");
        AddKeyValue(section, "Resident phone", values["PhoneNumber"]);
        if (!string.IsNullOrWhiteSpace(values["PostOfficeBox"]) && values["PostOfficeBox"] != "—")
        {
            AddKeyValue(section, "Post office box", values["PostOfficeBox"]);
        }

        AddSpacer(section, 8f);

        var termSentence = monthToMonth
            ? "The term of lease shall be one calendar month. The lease shall be automatically renewed for additional periods of one month each until terminated by either party as prescribed herein."
            : "The term of lease shall be one calendar year. The lease shall be automatically renewed for additional periods of one year each until terminated by either party as prescribed herein.";

        AddSectionParagraph(
            section,
            "1. Parties and premises.",
            $"On this {values["AgreementDay"]} day of {values["AgreementMonthYear"]}, the Housing Authority of the Town of Wiley, referred to as Owner or Landlord, does hereby lease the apartment at {values["PremisesAddress"]} to {values["ResidentName"]}, referred to as Resident, under the terms and conditions stated herein.");

        AddSectionParagraph(
            section,
            "2. Household members.",
            $"Household members are: {values["HouseholdMembers"]}. Post office box: {values["PostOfficeBox"]}. Phone number: {values["PhoneNumber"]}. Apartment number: {values["ApartmentNumber"]}.");

        AddSectionParagraph(
            section,
            "3. Initial period and rent.",
            $"The lease shall begin on {data.LeaseStart:MMMM d, yyyy} and end on {data.LeaseEnd:MMMM d, yyyy}. The rent for this initial period is ${values["MonthlyRent"]}, payable in advance on the first day of each month beginning {data.RentStart:MMMM d, yyyy}. This rent shall remain in effect unless adjusted by the Owner after 30 days’ notice of rent change.");

        AddSectionParagraph(section, "4. Term of lease.", termSentence);

        AddSectionParagraph(
            section,
            "5. Late payment and returned check charges.",
            "Resident understands and agrees that if the total rent is not received by the fifth of each month, a $25.00 late charge will be due. If for any reason a rent check is returned by the Resident’s bank, Resident understands and agrees there will be a charge of $25.00 in addition to the rent due.");

        AddSectionParagraph(
            section,
            "6. Utilities, equipment, and services.",
            "Fees for water, sewer, and trash collection are included in the rent above and are the responsibility of the Owner. Electricity and natural gas, as well as any charges for cable television and telephone service, are the responsibility of the Resident. At no cost beyond the monthly rent specified above, the Owner agrees to furnish within the apartment a stove, dishwasher, washer/dryer, and refrigerator.");

        AddSectionParagraph(
            section,
            "7. Security deposit.",
            $"Resident has deposited ${values["SecurityDeposit"]} with the Owner as a security deposit to be used by the Owner at the termination of this lease toward reimbursement of the cost of repairing any intentional or negligent damages caused by the Resident, other household members, and their visitors, and any rent or other charges owed by the Resident. Resident will give thirty days’ written notice of move-out and make provisions for a move-out inspection by the Owner to be eligible for any refund of the security deposit, unless the Resident was unable to give notice for reasons beyond their control. The notice will include a forwarding address. All keys to the apartment and storage unit will be returned at or before the move-out inspection. The Owner agrees to return the security deposit to the Resident within thirty days after move-out, less any cost needed to pay: (1) damages beyond normal wear and tear; (2) charges accrued, including late charges and returned-check fees; (3) charges for unreturned keys; and (4) unpaid rent.");

        AddSectionParagraph(section, "8. Resident responsibilities.", "The Resident agrees to:");
        AddBullet(section, "Keep the apartment clean.");
        AddBullet(section, "Use all utilities, appliances, fixtures, and the apartment in a safe manner and only for the purposes for which they are intended.");
        AddBullet(section, "Not litter, nor deface, damage, or remove any part of the apartment, the grounds, or common areas of Brookside Community Living.");
        AddBullet(section, "Not make any repairs or changes to the apartment without the permission of the Town Clerk.");
        AddBullet(section, "Give the Owner notice of any defects in the plumbing, appliances, mechanical systems, or structure of the apartment or rented facilities.");
        AddBullet(
            section,
            "Allow access to the apartment at all reasonable times with reasonable notice by the Owner for quarterly inspections, making repairs or improvements, showing prospective renters after a 30-day notice has been given, or in the event of an emergency or apparent abandonment.");
        AddBullet(section, "Not assign this lease or sublet the apartment, or allow any person other than those listed in this lease to occupy the apartment, without written consent of the Owner.");
        AddBullet(section, "Not alter the apartment or the grounds in any way.");
        AddBullet(section, "Not use the apartment for unlawful purposes or engage in or permit unlawful activities in or around Brookside Community Living.");
        AddBullet(section, "Be responsible for insuring personal property brought within Brookside Community Living.");

        AddSectionParagraph(section, "9. Owner responsibilities.", "The Owner agrees to:");
        AddBullet(section, "Regularly clean and maintain Brookside Community Living in a safe and decent manner.");
        AddBullet(section, "Arrange for garbage collection and removal.");
        AddBullet(section, "Maintain all equipment in a safe and working condition.");
        AddBullet(section, "Make necessary repairs with reasonable promptness.");
        AddBullet(section, "Provide extermination if necessary.");
        AddBullet(section, "Maintain secure lighting.");
        AddBullet(section, "Give, whenever practical, 24 hours’ notice prior to any access to Resident’s apartment.");

        AddSectionParagraph(
            section,
            "10. Rules of occupancy.",
            "Resident agrees to abide by resident rules, which are Attachment One to this lease. Resident agrees to any amendment of such rules of occupancy by the Owner as long as 30 days’ notice is given by the Owner, any change is addressed to the safety and quiet and peaceful enjoyment of the complex by all residents, and the rules are reasonable for all residents to obey.");

        AddSectionParagraph(
            section,
            "11. Termination of lease.",
            "To terminate the lease, the Resident must give the Owner a 30-day written notice before moving from the apartment. If no notice is given, the Resident will be liable for rent up to the end of the thirty days that the notice was required, or to the day the apartment is re-rented, whichever date comes first. The Owner may terminate the lease only for: (1) the Resident’s material noncompliance with the terms of this lease; (2) the Resident’s failure to carry out obligations under Colorado state law and local law; or (3) other good cause, which includes but is not limited to the Resident’s refusal to accept the Owner’s changes to this lease. For terminations by the Owner, the Owner agrees to give the Resident written notice of the proposed termination in accordance with any time frames set forth in state and local law. The written notice must: (1) specify the date the lease will be terminated; (2) state the grounds for termination; (3) advise the Resident that he or she has ten days from the day after the notice is given or mailed to discuss the termination with the Owner; and (4) advise the Resident of his or her right to defend the action in court.");

        AddSectionParagraph(
            section,
            "12. Pets.",
            "Pets are NOT allowed to live at Brookside Community Living.");

        AddBody(
            section,
            "This lease and its attachments make up the entire agreement between the Resident and the Wiley Housing Authority, and shall not be changed, modified, or discharged in whole or in part except by a written agreement signed by Resident and Owner. If any portion of this lease is declared by a court to be invalid or illegal, all other terms of this lease will remain in effect and both the Owner and the Resident will continue to be bound by them.",
            after: 18f);

        AddSignatureBlock(section);
        return document;
    }

    private static bool IsMonthToMonthTemplate(Stream templateDocx)
    {
        // Peek is not reliable on non-seekable streams; year is the default.
        // Callers that know the file name should use Generate(..., fileName, ...).
        if (templateDocx.CanSeek)
        {
            var pos = templateDocx.Position;
            try
            {
                using var document = new WordDocument(templateDocx, FormatType.Docx);
                foreach (WSection section in document.Sections)
                {
                    foreach (WParagraph paragraph in section.Paragraphs)
                    {
                        if (paragraph.Text.Contains("one calendar month", StringComparison.OrdinalIgnoreCase))
                        {
                            return true;
                        }
                    }
                }
            }
            finally
            {
                templateDocx.Position = pos;
            }
        }

        return false;
    }

    private static void AddCentered(IWSection section, string text, float size, bool bold = false, float after = 2f)
    {
        var p = section.AddParagraph();
        p.ParagraphFormat.HorizontalAlignment = HorizontalAlignment.Center;
        p.ParagraphFormat.AfterSpacing = after;
        p.ParagraphFormat.BeforeSpacing = 0;
        AppendText(p, text, size, bold);
    }

    private static void AddKeyValue(IWSection section, string label, string value)
    {
        var p = section.AddParagraph();
        p.ParagraphFormat.AfterSpacing = 2f;
        p.ParagraphFormat.BeforeSpacing = 0;
        AppendText(p, $"{label}: ", BodySize, bold: true);
        AppendText(p, value, BodySize);
    }

    private static void AddSectionParagraph(IWSection section, string heading, string body)
    {
        var p = section.AddParagraph();
        p.ParagraphFormat.AfterSpacing = 8f;
        p.ParagraphFormat.BeforeSpacing = 6f;
        p.ParagraphFormat.HorizontalAlignment = HorizontalAlignment.Justify;
        AppendText(p, heading + " ", BodySize, bold: true);
        AppendText(p, body, BodySize);
    }

    private static void AddBody(IWSection section, string body, float after = 8f)
    {
        var p = section.AddParagraph();
        p.ParagraphFormat.AfterSpacing = after;
        p.ParagraphFormat.BeforeSpacing = 6f;
        p.ParagraphFormat.HorizontalAlignment = HorizontalAlignment.Justify;
        AppendText(p, body, BodySize);
    }

    private static void AddBullet(IWSection section, string text)
    {
        var p = section.AddParagraph();
        p.ParagraphFormat.LeftIndent = 22f;
        p.ParagraphFormat.FirstLineIndent = -12f;
        p.ParagraphFormat.AfterSpacing = 3f;
        AppendText(p, "•  " + text, BodySize);
    }

    private static void AddSpacer(IWSection section, float after)
    {
        var p = section.AddParagraph();
        p.ParagraphFormat.AfterSpacing = after;
    }

    private static void AddRule(IWSection section)
    {
        var p = section.AddParagraph();
        p.ParagraphFormat.AfterSpacing = 10f;
        p.ParagraphFormat.Borders.Bottom.BorderType = BorderStyle.Single;
        p.ParagraphFormat.Borders.Bottom.Color = Color.FromArgb(255, 31, 107, 92);
        p.ParagraphFormat.Borders.Bottom.LineWidth = 1.25f;
        p.ParagraphFormat.Borders.Space = 1f;
    }

    private static void AddSignatureBlock(IWSection section)
    {
        AddSpacer(section, 12f);
        var resident = section.AddParagraph();
        resident.ParagraphFormat.AfterSpacing = 4f;
        AppendText(resident, "Resident signature", BodySize, bold: true);

        var residentLine = section.AddParagraph();
        residentLine.ParagraphFormat.AfterSpacing = 2f;
        AppendText(residentLine, "_______________________________________________", BodySize);
        var residentDate = section.AddParagraph();
        residentDate.ParagraphFormat.AfterSpacing = 16f;
        AppendText(residentDate, "Date: ____________________", BodySize);

        var owner = section.AddParagraph();
        owner.ParagraphFormat.AfterSpacing = 4f;
        AppendText(owner, "Owner signature (Wiley Housing Authority)", BodySize, bold: true);
        var ownerLine = section.AddParagraph();
        ownerLine.ParagraphFormat.AfterSpacing = 2f;
        AppendText(ownerLine, "_______________________________________________", BodySize);
        var byLine = section.AddParagraph();
        byLine.ParagraphFormat.AfterSpacing = 2f;
        AppendText(byLine, "By: ____________________________________________", BodySize);
        var ownerDate = section.AddParagraph();
        ownerDate.ParagraphFormat.AfterSpacing = 0;
        AppendText(ownerDate, "Date: ____________________", BodySize);
    }

    private static void AppendText(IWParagraph paragraph, string text, float size, bool bold = false, bool italic = false)
    {
        var range = paragraph.AppendText(text);
        range.CharacterFormat.FontName = BodyFont;
        range.CharacterFormat.FontSize = size;
        range.CharacterFormat.Bold = bold;
        range.CharacterFormat.Italic = italic;
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

    /// <summary>Legacy helper retained for binary compatibility with prior tests.</summary>
    internal static byte[] RewriteSectionTwo(byte[] pdf, LeaseMergeData data) => pdf;

    /// <summary>Legacy helper — professional layout already includes letterhead.</summary>
    internal static byte[] PrependLetterheadPage(byte[] pdf, LeaseMergeData data) => pdf;
}
