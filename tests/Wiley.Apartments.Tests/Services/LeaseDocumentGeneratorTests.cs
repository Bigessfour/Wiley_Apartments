using Syncfusion.Pdf.Parsing;
using Wiley.Apartments.Contracts;
using Wiley.Apartments.Web.Services;

namespace Wiley.Apartments.Tests.Services;

public class LeaseDocumentGeneratorTests
{
    private static string? ResolveTemplatePath()
    {
        var candidates = new[]
        {
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "local-docs", "templates", "brookside-year-lease.docx")),
            Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "local-docs", "templates", "brookside-year-lease.docx")),
            Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "local-docs", "templates", "brookside-year-lease.docx"))
        };

        return candidates.FirstOrDefault(File.Exists);
    }

    private static LeaseMergeData SampleData() => new()
    {
        AgreementDate = new DateTime(2026, 8, 9),
        PremisesAddress = "Unit 3, Brookside Community Living, Wiley, CO",
        ResidentName = "Pat Nguyen",
        HouseholdMembers = "Pat Nguyen, Sam Nguyen",
        PostOfficeBox = "PO Box 12",
        PhoneNumber = "719-555-0100",
        ApartmentNumber = "3",
        LeaseStart = new DateTime(2026, 9, 1),
        LeaseEnd = new DateTime(2027, 8, 31),
        MonthlyRent = 650m,
        RentStart = new DateTime(2026, 9, 1),
        SecurityDeposit = 650m
    };

    [Fact]
    public void CreateFillablePdfFromDocx_PlacesNamedAcroFormFields()
    {
        var templatePath = ResolveTemplatePath();
        if (templatePath is null)
        {
            return;
        }

        var generator = new LeaseDocumentGenerator();
        using var template = File.OpenRead(templatePath);
        var fillable = generator.CreateFillablePdfFromDocx(template);

        fillable.Length.Should().BeGreaterThan(1000);
        using var loaded = new PdfLoadedDocument(fillable);
        loaded.Form.Fields.Count.Should().BeGreaterThan(5);
        loaded.Form.Fields.Cast<Syncfusion.Pdf.Interactive.PdfField>()
            .Select(f => f.Name)
            .Should().Contain(["ResidentName", "MonthlyRent", "SecurityDeposit"]);
    }

    [Fact]
    public void Generate_FromDocx_ProducesFilledPdfWithTenantData()
    {
        var templatePath = ResolveTemplatePath();
        if (templatePath is null)
        {
            return;
        }

        var generator = new LeaseDocumentGenerator();
        using var template = File.OpenRead(templatePath);
        var (docx, pdf) = generator.Generate(template, "brookside-year-lease.docx", SampleData());

        pdf.Length.Should().BeGreaterThan(1000);
        docx.Should().NotBeNull();
        using var loaded = new PdfLoadedDocument(pdf);
        // Flattened fill — extract text
        var text = string.Join('\n', Enumerable.Range(0, loaded.Pages.Count)
            .Select(i => loaded.Pages[i].ExtractText()));
        text.Should().Contain("Pat Nguyen");
        text.Should().Contain("650.00");
    }

    [Fact]
    public void Generate_WithCustomClauses_AppendsAddendumPage()
    {
        var templatePath = ResolveTemplatePath();
        if (templatePath is null)
        {
            return;
        }

        var data = SampleData();
        data.CustomClauses = "Pets require prior written approval from the Housing Authority.";
        var generator = new LeaseDocumentGenerator();
        using var template = File.OpenRead(templatePath);
        var (_, pdf) = generator.Generate(template, "brookside-year-lease.docx", data);

        using var loaded = new PdfLoadedDocument(pdf);
        loaded.Pages.Count.Should().BeGreaterThan(4);
        var lastText = loaded.Pages[loaded.Pages.Count - 1].ExtractText();
        lastText.Should().Contain("Additional Clauses");
        lastText.Should().Contain("Pets require prior written approval");
    }

    [Fact]
    public void FillPdf_SetsAcroFormValues()
    {
        var templatePath = ResolveTemplatePath();
        if (templatePath is null)
        {
            return;
        }

        var generator = new LeaseDocumentGenerator();
        using var template = File.OpenRead(templatePath);
        var fillable = generator.CreateFillablePdfFromDocx(template);
        using var fillableStream = new MemoryStream(fillable);
        var filled = generator.FillPdf(fillableStream, SampleData(), flatten: false);

        using var loaded = new PdfLoadedDocument(filled);
        var resident = loaded.Form.Fields
            .Cast<Syncfusion.Pdf.Interactive.PdfField>()
            .First(f => f.Name == "ResidentName") as Syncfusion.Pdf.Parsing.PdfLoadedTextBoxField;
        resident.Should().NotBeNull();
        resident!.Text.Should().Contain("Pat Nguyen");
    }
}
