namespace VeloxPdf.Tests.Document;
using VeloxPdf.Document;
using VeloxPdf.Fonts;
using Xunit;

public class PdfDocumentTests
{
    [Fact]
    public void NewDocument_HasNoPagesInitially()
    {
        var doc = new PdfDocument();
        Assert.Empty(doc.Pages);
    }

    [Fact]
    public void AddPage_IncreasesPageCount()
    {
        var doc = new PdfDocument();
        doc.AddPage();
        Assert.Single(doc.Pages);
        doc.AddPage();
        Assert.Equal(2, doc.Pages.Count);
    }

    [Fact]
    public void Save_ProducesValidPdfHeader()
    {
        var doc = new PdfDocument();
        doc.AddPage();
        byte[] bytes = doc.Save();
        string header = System.Text.Encoding.ASCII.GetString(bytes, 0, 5);
        Assert.Equal("%PDF-", header);
    }

    [Fact]
    public void Save_ContainsEOFMarker()
    {
        var doc = new PdfDocument();
        doc.AddPage();
        byte[] bytes = doc.Save();
        string content = System.Text.Encoding.GetEncoding("iso-8859-1").GetString(bytes);
        Assert.Contains("%%EOF", content);
    }

    [Fact]
    public void Save_ContainsCatalog()
    {
        var doc = new PdfDocument();
        doc.AddPage();
        byte[] bytes = doc.Save();
        string content = System.Text.Encoding.GetEncoding("iso-8859-1").GetString(bytes);
        Assert.Contains("/Catalog", content);
    }

    [Fact]
    public void Save_ContainsXRef()
    {
        var doc = new PdfDocument();
        doc.AddPage();
        byte[] bytes = doc.Save();
        string content = System.Text.Encoding.GetEncoding("iso-8859-1").GetString(bytes);
        Assert.Contains("xref", content);
        Assert.Contains("startxref", content);
    }

    [Fact]
    public void DefaultFont_IsHelvetica()
    {
        var doc = new PdfDocument();
        Assert.Equal("Helvetica", doc.DefaultFont.BaseFont);
    }

    [Fact]
    public void SaveWithMetadata_IncludesTitleInOutput()
    {
        var doc = new PdfDocument();
        doc.Metadata.Title = "Test Document";
        doc.AddPage();
        byte[] bytes = doc.Save();
        string content = System.Text.Encoding.GetEncoding("iso-8859-1").GetString(bytes);
        Assert.Contains("Test Document", content);
    }

    [Fact]
    public async Task SaveAsync_ProducesValidOutput()
    {
        var doc = new PdfDocument();
        doc.AddPage();
        using var ms = new MemoryStream();
        await doc.SaveAsync(ms);
        byte[] bytes = ms.ToArray();
        string header = System.Text.Encoding.ASCII.GetString(bytes, 0, 5);
        Assert.Equal("%PDF-", header);
    }

    [Fact]
    public void AddPage_WithCustomSize_CreatesCorrectDimensions()
    {
        var doc = new PdfDocument();
        var page = doc.AddPage(PdfPageSize.Letter);
        Assert.Equal(612f, page.Width, 1f);
        Assert.Equal(792f, page.Height, 1f);
    }

    [Fact]
    public void Save_WithNoPages_CreatesOnePageAutomatically()
    {
        var doc = new PdfDocument();
        // Don't add pages explicitly
        byte[] bytes = doc.Save();
        Assert.NotNull(bytes);
        Assert.True(bytes.Length > 0);
        string header = System.Text.Encoding.ASCII.GetString(bytes, 0, 5);
        Assert.Equal("%PDF-", header);
    }

    [Fact]
    public void Save_ContainsPages()
    {
        var doc = new PdfDocument();
        doc.AddPage();
        byte[] bytes = doc.Save();
        string content = System.Text.Encoding.GetEncoding("iso-8859-1").GetString(bytes);
        Assert.Contains("/Pages", content);
    }

    [Fact]
    public void DefaultPageSize_IsA4()
    {
        var doc = new PdfDocument();
        Assert.Equal(PdfPageSize.A4.Width, doc.DefaultPageSize.Width, 1f);
        Assert.Equal(PdfPageSize.A4.Height, doc.DefaultPageSize.Height, 1f);
    }
}
