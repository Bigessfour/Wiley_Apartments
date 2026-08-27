using Wiley.Apartments.Web.Services;

namespace Wiley.Apartments.Tests.Services;

public class LeaseDocumentFileNameTests
{
    [Fact]
    public void BuildBaseName_UsesLastFirstUnitAndStartDate()
    {
        LeaseDocumentFileName.BuildBaseName("McKitrick", "Stephen", "1", new DateTime(2026, 8, 26))
            .Should().Be("McKitrick-Stephen-Unit1-2026-08-26");
    }

    [Fact]
    public void PdfFileName_AddsPdfExtension()
    {
        LeaseDocumentFileName.PdfFileName("Nguyen", "Pat", "3", new DateTime(2026, 9, 1))
            .Should().Be("Nguyen-Pat-Unit3-2026-09-01.pdf");
    }

    [Fact]
    public void BuildBaseName_StripsPunctuationAndSpaces()
    {
        LeaseDocumentFileName.BuildBaseName("O'Brien", "Mary Jane", "3B", new DateTime(2026, 1, 5))
            .Should().Be("OBrien-MaryJane-Unit3B-2026-01-05");
    }

    [Fact]
    public void BuildBaseName_DoesNotDoubleUnitPrefix()
    {
        LeaseDocumentFileName.BuildBaseName("Smith", "Ann", "Unit2", new DateTime(2026, 2, 1))
            .Should().Be("Smith-Ann-Unit2-2026-02-01");
    }
}
