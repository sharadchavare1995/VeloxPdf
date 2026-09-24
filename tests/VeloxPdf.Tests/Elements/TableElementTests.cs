namespace VeloxPdf.Tests.Elements;
using VeloxPdf.Document;
using VeloxPdf.Elements;
using VeloxPdf.Fonts;
using VeloxPdf.Layout;
using VeloxPdf.Styling;
using Xunit;

public class TableElementTests
{
    private static RenderContext CreateContext(PdfDocument doc, PdfPage page)
    {
        var layout = new LayoutEngine(page.Width, page.Height, page.Margins);
        return new RenderContext(doc, page, layout, Type1Font.Helvetica, 10f);
    }

    [Fact]
    public void TableWithRows_Renders_WithoutException()
    {
        var doc = new PdfDocument();
        var page = doc.AddPage();
        var ctx = CreateContext(doc, page);

        var table = new TableElement
        {
            Headers = ["Name", "Age", "City"],
            Rows = [["Alice", "30", "New York"], ["Bob", "25", "London"]]
        };

        var ex = Record.Exception(() => table.Render(ctx));
        Assert.Null(ex);
    }

    [Fact]
    public void TableRender_AdvancesCursor()
    {
        var doc = new PdfDocument();
        var page = doc.AddPage();
        var ctx = CreateContext(doc, page);
        float before = ctx.Layout.CursorY;

        var table = new TableElement
        {
            Headers = ["A", "B"],
            Rows = [["1", "2"], ["3", "4"]]
        };
        table.Render(ctx);

        Assert.True(ctx.Layout.CursorY > before);
    }

    [Fact]
    public void MeasureHeight_IncludesHeaderAndRows()
    {
        var doc = new PdfDocument();
        var page = doc.AddPage();
        var ctx = CreateContext(doc, page);

        var tableNoRows = new TableElement
        {
            Headers = ["Col1", "Col2"],
            Rows = []
        };
        var tableWithRows = new TableElement
        {
            Headers = ["Col1", "Col2"],
            Rows = [["A", "B"], ["C", "D"]]
        };

        float heightNoRows = tableNoRows.MeasureHeight(ctx);
        float heightWithRows = tableWithRows.MeasureHeight(ctx);

        Assert.True(heightWithRows > heightNoRows);
    }

    [Fact]
    public void CanSplit_IsTrue()
    {
        var table = new TableElement();
        Assert.True(table.CanSplit);
    }

    [Fact]
    public void EmptyHeaders_DoesNotThrow()
    {
        var doc = new PdfDocument();
        var page = doc.AddPage();
        var ctx = CreateContext(doc, page);

        var table = new TableElement
        {
            Headers = [],
            Rows = [["A", "B"]]
        };
        var ex = Record.Exception(() => table.Render(ctx));
        Assert.Null(ex);
    }

    [Fact]
    public void ManyRows_DoesNotThrow()
    {
        var doc = new PdfDocument();
        var page = doc.AddPage();
        var ctx = CreateContext(doc, page);

        var rows = Enumerable.Range(1, 100)
            .Select(i => new[] { $"Row {i}", $"Value {i}" })
            .ToList();

        var table = new TableElement
        {
            Headers = ["Name", "Value"],
            Rows = rows
        };

        var ex = Record.Exception(() => table.Render(ctx));
        Assert.Null(ex);
    }

    [Fact]
    public void SingleRow_SingleColumn_Renders()
    {
        var doc = new PdfDocument();
        var page = doc.AddPage();
        var ctx = CreateContext(doc, page);

        var table = new TableElement
        {
            Headers = ["Col"],
            Rows = [["Data"]]
        };
        var ex = Record.Exception(() => table.Render(ctx));
        Assert.Null(ex);
    }

    // -------------------------------------------------------------------------
    // Header-height / body-row positioning tests
    // -------------------------------------------------------------------------

    /// <summary>
    /// When a header cell wraps across multiple lines the header row height must
    /// grow to accommodate all lines.  Body rows must start BELOW the fully-drawn
    /// header — the cursor advance after the header must equal the dynamic height,
    /// not some smaller fixed or single-line value.
    /// </summary>
    [Fact]
    public void WrappedHeader_BodyRows_StartBelowHeader()
    {
        var doc = new PdfDocument();
        var page = doc.AddPage();
        var ctx = CreateContext(doc, page);

        // Force a very narrow column so the long header text wraps.
        var table = new TableElement
        {
            Headers = ["Very Long Column Header That Must Wrap", "B"],
            Rows = [["data1", "data2"]],
            ColumnWidths = [0.2f, 0.8f]   // 20 % / 80 % — first column is tiny
        };

        float before = ctx.Layout.CursorY;
        table.Render(ctx);
        float after = ctx.Layout.CursorY;

        // Compute what a single-line header height would be.
        float fontSize = table.Style.HeaderTextStyle.FontSize ?? 10f;
        float lineH = fontSize * 1.2f;
        float padV = table.Style.CellPaddingTop + table.Style.CellPaddingBottom;
        float singleLineHeaderH = lineH + padV;

        // The cursor must have advanced by more than a single-line header +
        // a single body row, proving the header row expanded for the wrapped text.
        float singleLineBodyH = lineH + padV;   // "data1" won't wrap
        float minExpected = singleLineHeaderH * 2 + singleLineBodyH; // ≥ 2 header lines

        Assert.True(after - before >= minExpected,
            $"Expected cursor advance ≥ {minExpected} pt (header wraps), got {after - before} pt.");
    }

    /// <summary>
    /// When Style.HeaderHeight is set to a fixed value that is SMALLER than the
    /// height required by wrapped header text, ComputeHeaderHeight must return the
    /// dynamic (larger) height so body rows are not drawn over header text.
    /// </summary>
    [Fact]
    public void FixedHeaderHeight_TooSmallForWrapping_DynamicHeightUsed()
    {
        var doc = new PdfDocument();
        var page = doc.AddPage();
        var ctx = CreateContext(doc, page);

        // Narrow first column forces header to wrap; fixed HeaderHeight is tiny.
        var table = new TableElement
        {
            Headers = ["Wrapped Header Text Here", "B"],
            Rows = [],
            ColumnWidths = [0.15f, 0.85f],
            Style = new TableStyle { HeaderHeight = 5f }   // intentionally tiny
        };

        float height = table.MeasureHeight(ctx);

        // A 5 pt header is far too small for any readable wrapped text.
        // MeasureHeight must return something larger.
        Assert.True(height > 5f,
            $"MeasureHeight should exceed the tiny fixed HeaderHeight (5 pt); got {height} pt.");
    }

    /// <summary>
    /// MeasureHeight must use the header font (not the data font) when computing
    /// the header row height, so that the measured total matches what Render draws.
    /// </summary>
    [Fact]
    public void MeasureHeight_UsesHeaderFont_NotDataFont()
    {
        var doc = new PdfDocument();
        var page = doc.AddPage();
        var ctx = CreateContext(doc, page);

        var tableDefaultFonts = new TableElement
        {
            Headers = ["Header"],
            Rows = [["Row"]]
        };

        // Manually compute expected: header uses HelveticaBold @ 10, data uses Helvetica @ 9.
        float hFontSize = tableDefaultFonts.Style.HeaderTextStyle.FontSize ?? 10f;
        float dFontSize = tableDefaultFonts.Style.DataTextStyle.FontSize ?? 9f;
        float hLineH = hFontSize * 1.2f;
        float dLineH = dFontSize * 1.2f;
        float padV = tableDefaultFonts.Style.CellPaddingTop + tableDefaultFonts.Style.CellPaddingBottom;

        float expectedHeaderH = hLineH + padV;
        float expectedRowH = dLineH + padV;
        float expectedTotal = expectedHeaderH + expectedRowH;

        float measured = tableDefaultFonts.MeasureHeight(ctx);

        Assert.True(Math.Abs(measured - expectedTotal) < 0.5f,
            $"MeasureHeight {measured} pt does not match expected {expectedTotal} pt " +
            $"(header font size {hFontSize} vs data font size {dFontSize}).");
    }
}
