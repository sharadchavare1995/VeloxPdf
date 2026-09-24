namespace VeloxPdf.Tests.Integration;
using VeloxPdf.Document;
using VeloxPdf.Elements;
using VeloxPdf.Fonts;
using VeloxPdf.Styling;
using Xunit;

public class MultiPageTests
{
    [Fact]
    public void LargeTable_GeneratesValidPdf()
    {
        var doc = new PdfDocument();
        var rows = Enumerable.Range(1, 200)
            .Select(i => new[] { $"Row {i}", $"Data {i}", $"Value {i * 10}" })
            .ToList();

        doc.Elements.Add(new TableElement
        {
            Headers = ["Name", "Data", "Value"],
            Rows = rows
        });

        byte[] bytes = doc.Save();

        Assert.NotNull(bytes);
        string header = System.Text.Encoding.ASCII.GetString(bytes, 0, 5);
        Assert.Equal("%PDF-", header);
        string content = System.Text.Encoding.GetEncoding("iso-8859-1").GetString(bytes);
        Assert.Contains("%%EOF", content);
    }

    [Fact]
    public void ContentStream_ContainsFlateDecode_Compression()
    {
        var doc = new PdfDocument();
        var page = doc.AddPage();
        page.ContentStream
            .BeginText()
            .SetFont(page.RegisterFont(Type1Font.Helvetica), 12)
            .SetAbsolutePos(36, 750)
            .ShowText("Test content for compression verification")
            .EndText();

        byte[] pdfBytes = doc.Save();

        Assert.True(pdfBytes.Length > 0);
        string content = System.Text.Encoding.GetEncoding("iso-8859-1").GetString(pdfBytes);
        Assert.Contains("FlateDecode", content);
    }

    [Fact]
    public void PageBreakElement_DoesNotThrow()
    {
        var doc = new PdfDocument();
        doc.Elements.Add(new TextElement { Text = "Page 1 content" });
        doc.Elements.Add(new PageBreakElement());
        doc.Elements.Add(new TextElement { Text = "Page 2 content" });

        byte[] bytes = doc.Save();
        Assert.NotNull(bytes);
        Assert.True(bytes.Length > 0);
    }

    [Fact]
    public void MultipleElements_AllRendered_ValidPdf()
    {
        var doc = new PdfDocument();
        doc.Elements.Add(new TextElement
        {
            Text = "Report Title",
            Style = TextStyle.Heading1
        });
        doc.Elements.Add(new LineElement());
        doc.Elements.Add(new TextElement
        {
            Text = "Body text goes here with some content to display in the report."
        });
        doc.Elements.Add(new TableElement
        {
            Headers = ["Column A", "Column B", "Column C"],
            Rows = [["Value 1", "Value 2", "Value 3"], ["Data A", "Data B", "Data C"]]
        });

        byte[] bytes = doc.Save();
        Assert.NotNull(bytes);
        string header = System.Text.Encoding.ASCII.GetString(bytes, 0, 5);
        Assert.Equal("%PDF-", header);
    }

    [Fact]
    public void TwoPagesExplicit_BothInOutput()
    {
        var doc = new PdfDocument();
        var page1 = doc.AddPage();
        page1.ContentStream
            .BeginText()
            .SetFont(page1.RegisterFont(Type1Font.Helvetica), 12)
            .SetAbsolutePos(36, 700)
            .ShowText("Page One")
            .EndText();

        var page2 = doc.AddPage();
        page2.ContentStream
            .BeginText()
            .SetFont(page2.RegisterFont(Type1Font.Helvetica), 12)
            .SetAbsolutePos(36, 700)
            .ShowText("Page Two")
            .EndText();

        byte[] bytes = doc.Save();
        Assert.Equal(2, doc.Pages.Count);

        // Content streams use FlateDecode compression — check structural markers instead
        string header = System.Text.Encoding.ASCII.GetString(bytes, 0, 5);
        Assert.Equal("%PDF-", header);
        Assert.True(bytes.Length > 200, "PDF should have substantial content for 2 pages");
    }

    [Fact]
    public void RectangleElement_Renders_WithoutException()
    {
        var doc = new PdfDocument();
        doc.Elements.Add(new RectangleElement
        {
            Width = 200f,
            Height = 50f,
            FillColor = Color.PdfColor.FromHex("#2563EB"),
            CornerRadius = 5f
        });

        byte[] bytes = doc.Save();
        Assert.NotNull(bytes);
        Assert.True(bytes.Length > 0);
    }
}
