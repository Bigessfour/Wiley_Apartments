using Syncfusion.Drawing;
using Syncfusion.Pdf;
using Syncfusion.Pdf.Graphics;
using Syncfusion.Pdf.Interactive;
using Syncfusion.Pdf.Parsing;
using Wiley.Apartments.Contracts;

namespace Wiley.Apartments.Web.Services;

/// <summary>
/// Fills the Town of Wiley AcroForm payment receipt template (NV-1).
/// Prefers <c>DocumentRoot/templates/Wiley_Payment_Receipt_Template.pdf</c>;
/// falls back to the shipped Templates folder, then a simple drawn PDF.
/// </summary>
public sealed class PaymentReceiptGenerator(
    IDocumentPathResolver paths,
    IHostEnvironment env,
    ILogger<PaymentReceiptGenerator> logger)
{
    public const string TemplateFileName = "Wiley_Payment_Receipt_Template.pdf";

    private readonly IDocumentPathResolver _paths = paths;
    private readonly IHostEnvironment _env = env;
    private readonly ILogger<PaymentReceiptGenerator> _logger = logger;

    /// <summary>Parameterless ctor for unit tests that only exercise the drawn fallback.</summary>
    internal PaymentReceiptGenerator()
        : this(
            new FixedDocumentRoot(Path.GetTempPath()),
            new EmptyHostEnvironment(),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<PaymentReceiptGenerator>.Instance)
    {
    }

    public byte[] Generate(PaymentReceiptData data, bool flatten = true)
    {
        var templatePath = ResolveTemplatePath();
        if (templatePath is not null)
        {
            try
            {
                using var stream = File.OpenRead(templatePath);
                return FillPdf(stream, data, flatten);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Failed to fill receipt template at {TemplatePath}; using drawn fallback.",
                    templatePath);
            }
        }
        else
        {
            _logger.LogWarning(
                "Receipt template {FileName} not found under DocumentRoot/templates or app Templates; using drawn fallback.",
                TemplateFileName);
        }

        return GenerateDrawn(data);
    }

    public byte[] FillPdf(Stream fillablePdfTemplate, PaymentReceiptData data, bool flatten = true)
    {
        using var loaded = new PdfLoadedDocument(fillablePdfTemplate);
        if (loaded.Form is null || loaded.Form.Fields.Count == 0)
        {
            throw new InvalidOperationException("Payment receipt PDF template has no AcroForm fields.");
        }

        var values = ToFieldValues(data);
        foreach (PdfField field in loaded.Form.Fields)
        {
            if (!values.TryGetValue(field.Name, out var value))
            {
                continue;
            }

            switch (field)
            {
                case PdfLoadedTextBoxField textBox:
                    textBox.Text = value;
                    break;
                case PdfLoadedComboBoxField combo:
                    combo.SelectedValue = value;
                    break;
                case PdfTextBoxField createdText:
                    createdText.Text = value;
                    break;
                case PdfComboBoxField createdCombo:
                    createdCombo.SelectedValue = value;
                    break;
            }
        }

        if (flatten)
        {
            loaded.Form.Flatten = true;
        }

        using var output = new MemoryStream();
        loaded.Save(output);
        return output.ToArray();
    }

    /// <summary>
    /// Ensures DocumentRoot/templates has the blank receipt PDF (copies from app Templates if missing).
    /// </summary>
    public void EnsureTemplateOnDocumentRoot()
    {
        var root = _paths.GetDocumentRoot();
        var destDir = Path.Combine(root, "templates");
        var dest = Path.Combine(destDir, TemplateFileName);
        if (File.Exists(dest))
        {
            return;
        }

        var bundled = Path.Combine(_env.ContentRootPath, "Templates", TemplateFileName);
        if (!File.Exists(bundled))
        {
            bundled = Path.Combine(AppContext.BaseDirectory, "Templates", TemplateFileName);
        }

        if (!File.Exists(bundled))
        {
            return;
        }

        Directory.CreateDirectory(destDir);
        File.Copy(bundled, dest, overwrite: false);
        _logger.LogInformation("Seeded payment receipt template to {Dest}.", dest);
    }

    private string? ResolveTemplatePath()
    {
        EnsureTemplateOnDocumentRoot();

        var candidates = new[]
        {
            Path.Combine(_paths.GetDocumentRoot(), "templates", TemplateFileName),
            Path.Combine(_env.ContentRootPath, "Templates", TemplateFileName),
            Path.Combine(AppContext.BaseDirectory, "Templates", TemplateFileName),
        };

        return candidates.FirstOrDefault(File.Exists);
    }

    private static Dictionary<string, string> ToFieldValues(PaymentReceiptData data) =>
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["ReceiptNumber"] = data.ReceiptNumber,
            ["ReceiptDate"] = data.ReceiptDate,
            ["TenantName"] = data.TenantName,
            ["UnitNumber"] = data.UnitNumber,
            ["PaymentType"] = data.PaymentType,
            ["Amount"] = data.Amount,
            ["PaymentMethod"] = data.PaymentMethod,
            ["ReferenceNumber"] = data.ReferenceNumber ?? string.Empty,
            ["Description"] = data.Description ?? string.Empty,
            ["Notes"] = data.Notes ?? string.Empty,
            ["ReceivedBy"] = data.ReceivedBy ?? string.Empty,
            ["Signature"] = data.Signature ?? string.Empty,
        };

    private static byte[] GenerateDrawn(PaymentReceiptData data)
    {
        using var doc = new PdfDocument();
        doc.PageSettings.Size = PdfPageSize.Letter;
        doc.PageSettings.Margins.All = 48;
        var page = doc.Pages.Add();
        var g = page.Graphics;

        var titleFont = new PdfStandardFont(PdfFontFamily.Helvetica, 18, PdfFontStyle.Bold);
        var headFont = new PdfStandardFont(PdfFontFamily.Helvetica, 11, PdfFontStyle.Bold);
        var bodyFont = new PdfStandardFont(PdfFontFamily.Helvetica, 11);
        var smallFont = new PdfStandardFont(PdfFontFamily.Helvetica, 9);
        var black = PdfBrushes.Black;
        var gray = new PdfSolidBrush(Color.FromArgb(255, 90, 90, 90));

        float y = 0;
        g.DrawString("Town of Wiley — Official Municipal Receipt", titleFont, black, new PointF(0, y));
        y += 28;
        g.DrawString("PAYMENT RECEIPT", headFont, black, new PointF(0, y));
        y += 22;
        g.DrawLine(new PdfPen(Color.FromArgb(255, 60, 60, 60), 1), 0, y, page.GetClientSize().Width, y);
        y += 16;

        void Row(string label, string value)
        {
            g.DrawString(label, headFont, gray, new PointF(0, y));
            g.DrawString(value, bodyFont, black, new PointF(160, y));
            y += 18;
        }

        Row("Receipt #", data.ReceiptNumber);
        Row("Date", data.ReceiptDate);
        Row("Tenant", data.TenantName);
        Row("Unit", data.UnitNumber);
        Row("Type", data.PaymentType);
        Row("Amount", $"${data.Amount}");
        Row("Method", data.PaymentMethod);
        if (!string.IsNullOrWhiteSpace(data.ReferenceNumber))
        {
            Row("Reference", data.ReferenceNumber);
        }

        if (!string.IsNullOrWhiteSpace(data.Description))
        {
            Row("Description", data.Description);
        }

        if (!string.IsNullOrWhiteSpace(data.Notes))
        {
            y += 6;
            Row("Notes", data.Notes);
        }

        if (!string.IsNullOrWhiteSpace(data.ReceivedBy))
        {
            y += 12;
            Row("Received by", data.ReceivedBy);
        }

        y += 28;
        g.DrawString(
            "Thank you. Keep this receipt for your records.",
            bodyFont,
            black,
            new PointF(0, y));
        y += 36;
        g.DrawString(
            "ClerkSuite · Town of Wiley apartments · Official record of payment received.",
            smallFont,
            gray,
            new PointF(0, y));

        using var stream = new MemoryStream();
        doc.Save(stream);
        return stream.ToArray();
    }

    private sealed class FixedDocumentRoot(string root) : IDocumentPathResolver
    {
        public string ConfiguredDefaultRoot => root;
        public string GetDocumentRoot() => root;
        public Task<string> GetDocumentRootAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(root);
        public Task SetDocumentRootAsync(string path, string? changedBy, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class EmptyHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Test";
        public string ApplicationName { get; set; } = "Test";
        public string ContentRootPath { get; set; } = Path.GetTempPath();
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } =
            new Microsoft.Extensions.FileProviders.NullFileProvider();
    }
}

public sealed record PaymentReceiptData(
    string ReceiptNumber,
    string ReceiptDate,
    string TenantName,
    string UnitNumber,
    string PaymentType,
    string Amount,
    string PaymentMethod,
    string? ReferenceNumber,
    string? Description,
    string? Notes,
    string? ReceivedBy,
    string? Signature);
