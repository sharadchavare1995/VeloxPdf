namespace VeloxPdf.Tests.Integration;
using VeloxPdf.Document;
using VeloxPdf.Templates;
using Xunit;

public class ReportGenerationTests
{
    [Fact]
    public void Report_Generates_ValidPdf()
    {
        var template = new ReportTemplate();
        var data = new TemplateData
        {
            Title = "Annual Report 2024",
            Metadata = new PdfMetadata { Author = "Test Author" }
        };
        data.Properties["ReportTitle"] = "Annual Report 2024";
        data.Properties["Author"] = "Test Team";
        data.Properties["Date"] = "January 1, 2024";

        var doc = template.Build(data);
        byte[] bytes = doc.Save();

        Assert.NotNull(bytes);
        string header = System.Text.Encoding.ASCII.GetString(bytes, 0, 5);
        Assert.Equal("%PDF-", header);
    }

    [Fact]
    public void Report_HasMultiplePages()
    {
        var template = new ReportTemplate();
        var sections = new List<ReportSection>
        {
            new("Intro", "Introduction text here with enough content to fill a section."),
            new("Analysis", "Analysis section content goes here with detailed analysis results."),
            new("Conclusion", "Final conclusion of the comprehensive report.")
        };

        var data = new TemplateData { Title = "Multi-page Report" };
        data.Data["Sections"] = sections;

        var doc = template.Build(data);
        Assert.True(doc.Pages.Count >= 2);
    }

    [Fact]
    public void Letter_Generates_ValidPdf()
    {
        var template = new LetterTemplate();
        var data = new TemplateData { Title = "Business Letter" };
        data.Properties["SenderName"] = "John Doe";
        data.Properties["RecipientName"] = "Jane Smith";
        data.Properties["Subject"] = "Important Business Matter";
        data.Properties["Body"] = "Dear Jane,\n\nI am writing to discuss an important matter.\n\nBest regards.";

        var doc = template.Build(data);
        byte[] bytes = doc.Save();

        Assert.NotNull(bytes);
        string header = System.Text.Encoding.ASCII.GetString(bytes, 0, 5);
        Assert.Equal("%PDF-", header);
    }

    [Fact]
    public void TemplateEngine_GeneratesViaEngine()
    {
        var registry = new TemplateRegistry();
        registry.Register(new InvoiceTemplate());
        registry.Register(new ReportTemplate());
        registry.Register(new LetterTemplate());

        var engine = new TemplateEngine(registry);

        var data = new TemplateData { Title = "Engine Test" };
        byte[] bytes = engine.Generate("Invoice", data);

        Assert.NotNull(bytes);
        Assert.True(bytes.Length > 0);
    }

    [Fact]
    public void TemplateEngine_ThrowsOnUnknownTemplate()
    {
        var registry = new TemplateRegistry();
        var engine = new TemplateEngine(registry);
        var data = new TemplateData { Title = "X" };

        Assert.Throws<Diagnostics.PdfException>(() => engine.Generate("NonExistent", data));
    }

    [Fact]
    public void TemplateRegistry_AvailableTemplates_ListsAll()
    {
        var registry = new TemplateRegistry();
        registry.Register(new InvoiceTemplate());
        registry.Register(new ReportTemplate());
        registry.Register(new LetterTemplate());

        var available = registry.AvailableTemplates;
        Assert.Equal(3, available.Count);
        Assert.Contains("Invoice", available);
        Assert.Contains("Report", available);
        Assert.Contains("Letter", available);
    }

    [Fact]
    public async Task TemplateEngine_GeneratesAsync()
    {
        var registry = new TemplateRegistry();
        registry.Register(new InvoiceTemplate());

        var engine = new TemplateEngine(registry);
        var data = new TemplateData { Title = "Async Test" };
        byte[] bytes = await engine.GenerateAsync("Invoice", data);

        Assert.NotNull(bytes);
        Assert.True(bytes.Length > 0);
        string header = System.Text.Encoding.ASCII.GetString(bytes, 0, 5);
        Assert.Equal("%PDF-", header);
    }
}
