namespace VeloxPdf.Tests.Integration;
using VeloxPdf.Document;
using VeloxPdf.Elements;
using VeloxPdf.Fonts;
using VeloxPdf.Styling;
using VeloxPdf.Templates;
using Xunit;

public class InvoiceGenerationTests
{
    [Fact]
    public void Invoice_GeneratesValidPdf_StartsWithPdfHeader()
    {
        var template = new InvoiceTemplate();
        var data = new TemplateData
        {
            Title = "Test Invoice",
            Metadata = new PdfMetadata { Title = "INV-001" }
        };
        data.Properties["CompanyName"] = "Acme Corp";
        data.Properties["InvoiceNumber"] = "INV-001";
        data.Properties["BillToName"] = "Test Client";

        var doc = template.Build(data);
        byte[] bytes = doc.Save();

        Assert.NotNull(bytes);
        Assert.True(bytes.Length > 0);
        string header = System.Text.Encoding.ASCII.GetString(bytes, 0, 5);
        Assert.Equal("%PDF-", header);
    }

    [Fact]
    public void Invoice_ContainsEofMarker()
    {
        var template = new InvoiceTemplate();
        var data = new TemplateData { Title = "Invoice" };
        var doc = template.Build(data);
        byte[] bytes = doc.Save();
        string content = System.Text.Encoding.GetEncoding("iso-8859-1").GetString(bytes);
        Assert.Contains("%%EOF", content);
    }

    [Fact]
    public void Invoice_HasAtLeastOnePage()
    {
        var template = new InvoiceTemplate();
        var data = new TemplateData { Title = "Invoice" };
        var doc = template.Build(data);
        Assert.True(doc.Pages.Count >= 1);
    }

    [Fact]
    public void Invoice_FileSize_IsReasonable()
    {
        var template = new InvoiceTemplate();
        var data = new TemplateData { Title = "Invoice" };
        var doc = template.Build(data);
        byte[] bytes = doc.Save();
        Assert.True(bytes.Length > 1000, $"File too small: {bytes.Length}");
        Assert.True(bytes.Length < 1_000_000, $"File too large: {bytes.Length}");
    }

    [Fact]
    public void PdfDocumentBuilder_CreatesValidDocument()
    {
        var doc = PdfDocumentBuilder.Create()
            .WithTitle("Builder Test")
            .WithAuthor("Test Author")
            .AddHeading("Test Heading")
            .AddText("Some body text for testing.")
            .Build();

        byte[] bytes = doc.Save();
        Assert.NotNull(bytes);
        Assert.True(bytes.Length > 0);
        string header = System.Text.Encoding.ASCII.GetString(bytes, 0, 5);
        Assert.Equal("%PDF-", header);
    }

    [Fact]
    public void Invoice_WithLineItems_RendersWithoutError()
    {
        var template = new InvoiceTemplate();
        var data = new TemplateData { Title = "Full Invoice" };
        data.Properties["CompanyName"] = "Acme Corporation";
        data.Properties["InvoiceNumber"] = "INV-2024-001";
        data.Properties["InvoiceDate"] = "Jan 1, 2024";
        data.Properties["DueDate"] = "Jan 31, 2024";
        data.Properties["BillToName"] = "Client Company";
        data.Properties["BillToAddress"] = "123 Client St";
        data.Data["LineItems"] = new List<string[]>
        {
            new string[] { "Web Development", "40", "150.00" },
            new string[] { "Design Services", "20", "100.00" },
            new string[] { "Hosting (Annual)", "1", "250.00" },
            new string[] { "Support Package", "1", "500.00" },
            new string[] { "Training Session", "2", "300.00" }
        };

        var doc = template.Build(data);
        byte[] bytes = doc.Save();

        Assert.NotNull(bytes);
        string header = System.Text.Encoding.ASCII.GetString(bytes, 0, 5);
        Assert.Equal("%PDF-", header);
    }

    [Fact]
    public void ParallelInvoiceGeneration_IsThreadSafe()
    {
        var results = new System.Collections.Concurrent.ConcurrentBag<bool>();
        var tasks = Enumerable.Range(1, 5).Select(i => Task.Run(() =>
        {
            var template = new InvoiceTemplate();
            var data = new TemplateData { Title = $"Invoice {i}" };
            data.Properties["InvoiceNumber"] = $"INV-{i:000}";
            var doc = template.Build(data);
            byte[] bytes = doc.Save();
            results.Add(bytes.Length > 1000);
        })).ToArray();

        Task.WaitAll(tasks);
        Assert.All(results, r => Assert.True(r));
    }
}
