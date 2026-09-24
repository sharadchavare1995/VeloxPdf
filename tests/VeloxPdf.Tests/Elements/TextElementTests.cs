namespace VeloxPdf.Tests.Elements;
using VeloxPdf.Document;
using VeloxPdf.Elements;
using VeloxPdf.Fonts;
using VeloxPdf.Layout;
using VeloxPdf.Styling;
using Xunit;

public class TextElementTests
{
    private static RenderContext CreateContext(PdfDocument doc, PdfPage page)
    {
        var layout = new LayoutEngine(page.Width, page.Height, page.Margins);
        return new RenderContext(doc, page, layout, Type1Font.Helvetica, 11f);
    }

    [Fact]
    public void MeasureHeight_SingleLine_ReturnsPositiveHeight()
    {
        var doc = new PdfDocument();
        var page = doc.AddPage();
        var ctx = CreateContext(doc, page);

        var elem = new TextElement { Text = "Short text" };
        float height = elem.MeasureHeight(ctx);
        Assert.True(height > 0);
    }

    [Fact]
    public void MeasureHeight_MultipleWords_GreaterThanSingleLine()
    {
        var doc = new PdfDocument();
        var page = doc.AddPage();
        var ctx = CreateContext(doc, page);

        string longText = string.Join(" ", Enumerable.Repeat("word", 100));
        var elem = new TextElement { Text = longText };
        float multiHeight = elem.MeasureHeight(ctx);

        var shortElem = new TextElement { Text = "word" };
        float singleHeight = shortElem.MeasureHeight(ctx);

        Assert.True(multiHeight > singleHeight);
    }

    [Fact]
    public void Render_DoesNotThrow()
    {
        var doc = new PdfDocument();
        var page = doc.AddPage();
        var ctx = CreateContext(doc, page);

        var elem = new TextElement
        {
            Text = "Hello, World!",
            Style = TextStyle.Default
        };
        var ex = Record.Exception(() => elem.Render(ctx));
        Assert.Null(ex);
    }

    [Fact]
    public void Render_AdvancesCursor()
    {
        var doc = new PdfDocument();
        var page = doc.AddPage();
        var ctx = CreateContext(doc, page);

        float beforeY = ctx.Layout.CursorY;
        var elem = new TextElement { Text = "Test" };
        elem.Render(ctx);
        Assert.True(ctx.Layout.CursorY > beforeY);
    }

    [Fact]
    public void CanSplit_IsTrue()
    {
        var elem = new TextElement();
        Assert.True(elem.CanSplit);
    }

    [Fact]
    public void Heading1Style_HasLargerFontSize()
    {
        // Heading1 must have a larger or different font than default
        Assert.True(
            TextStyle.Heading1.FontSize > (TextStyle.Default.FontSize ?? 11f) ||
            TextStyle.Heading1.Font != null);
    }

    [Fact]
    public void EmptyText_DoesNotThrow()
    {
        var doc = new PdfDocument();
        var page = doc.AddPage();
        var ctx = CreateContext(doc, page);

        var elem = new TextElement { Text = "" };
        var ex = Record.Exception(() => elem.Render(ctx));
        Assert.Null(ex);
    }

    [Fact]
    public void Render_WithBoldStyle_DoesNotThrow()
    {
        var doc = new PdfDocument();
        var page = doc.AddPage();
        var ctx = CreateContext(doc, page);

        var elem = new TextElement
        {
            Text = "Bold text here",
            Style = TextStyle.Default.WithBold()
        };
        var ex = Record.Exception(() => elem.Render(ctx));
        Assert.Null(ex);
    }

    [Fact]
    public void Render_WithCenterAlignment_DoesNotThrow()
    {
        var doc = new PdfDocument();
        var page = doc.AddPage();
        var ctx = CreateContext(doc, page);

        var elem = new TextElement
        {
            Text = "Centered text",
            Style = TextStyle.Default.WithAlignment(PdfAlignment.Center)
        };
        var ex = Record.Exception(() => elem.Render(ctx));
        Assert.Null(ex);
    }
}
