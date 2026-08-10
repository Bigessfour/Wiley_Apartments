using Wiley.Apartments.Web.Services;

namespace Wiley.Apartments.Tests.Services;

public class PaymentReceiptGeneratorTests
{
    private static readonly string TemplatePath = Path.GetFullPath(Path.Combine(
        AppContext.BaseDirectory,
        "..", "..", "..", "..", "..",
        "src", "Wiley.Apartments.Web", "Templates",
        PaymentReceiptGenerator.TemplateFileName));

    private static PaymentReceiptData SampleData() =>
        new(
            "WR-2026-0810-014",
            "08/10/2026",
            "Jane A. Ramirez",
            "7B",
            "Rent",
            "925.00",
            "Check",
            "4821",
            "Rent for August 2026",
            "Payment received in full. Thank you.",
            "Deb Dillon, Town Clerk",
            "/s/ Deb Dillon");

    [Fact]
    public void Generate_DrawnFallback_ReturnsNonEmptyPdf()
    {
        var gen = new PaymentReceiptGenerator();
        var bytes = gen.Generate(SampleData());

        bytes.Should().NotBeNullOrEmpty();
        System.Text.Encoding.ASCII.GetString(bytes.AsSpan(0, 4)).Should().Be("%PDF");
    }

    [Fact]
    public void FillPdf_FromWileyTemplate_ReturnsNonEmptyPdf()
    {
        File.Exists(TemplatePath).Should().BeTrue($"expected template at {TemplatePath}");

        var gen = new PaymentReceiptGenerator();
        using var stream = File.OpenRead(TemplatePath);
        var bytes = gen.FillPdf(stream, SampleData(), flatten: true);

        bytes.Should().NotBeNullOrEmpty();
        System.Text.Encoding.ASCII.GetString(bytes.AsSpan(0, 4)).Should().Be("%PDF");
        bytes.Length.Should().BeGreaterThan(5_000);
    }
}
