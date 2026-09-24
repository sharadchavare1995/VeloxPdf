namespace VeloxPdf.Tests.Layout;
using VeloxPdf.Document;
using VeloxPdf.Layout;
using Xunit;

public class LayoutEngineTests
{
    private static LayoutEngine CreateEngine() =>
        new LayoutEngine(595f, 842f, PdfMargins.Default);

    [Fact]
    public void ContentWidth_ExcludesMargins()
    {
        var engine = CreateEngine();
        Assert.Equal(595f - 36f - 36f, engine.ContentWidth, 1f);
    }

    [Fact]
    public void ContentHeight_ExcludesMargins()
    {
        var engine = CreateEngine();
        Assert.Equal(842f - 36f - 36f, engine.ContentHeight, 1f);
    }

    [Fact]
    public void ToPdfY_ConvertsTopDownToPdfCoords()
    {
        var engine = CreateEngine();
        float pdfY = engine.ToPdfY(0f);
        Assert.Equal(842f - 36f, pdfY, 1f);
    }

    [Fact]
    public void Advance_IncreasesCursorY()
    {
        var engine = CreateEngine();
        engine.Advance(50f);
        Assert.Equal(50f, engine.CursorY, 0.001f);
    }

    [Fact]
    public void CanFit_ReturnsTrueForSmallHeight()
    {
        var engine = CreateEngine();
        Assert.True(engine.CanFit(100f));
    }

    [Fact]
    public void CanFit_ReturnsFalseWhenPageFull()
    {
        var engine = CreateEngine();
        engine.Advance(engine.ContentHeight - 10f);
        Assert.False(engine.CanFit(50f));
    }

    [Fact]
    public void NewPage_ResetsCursorAndIncrementsPage()
    {
        var engine = CreateEngine();
        engine.Advance(200f);
        engine.NewPage();
        Assert.Equal(0f, engine.CursorY);
        Assert.Equal(2, engine.CurrentPage);
    }

    [Fact]
    public void RemainingHeight_DecreasesAfterAdvance()
    {
        var engine = CreateEngine();
        float initial = engine.RemainingHeight;
        engine.Advance(100f);
        Assert.Equal(initial - 100f, engine.RemainingHeight, 0.001f);
    }

    [Fact]
    public void Reset_RestoresCursorToZero()
    {
        var engine = CreateEngine();
        engine.Advance(300f);
        engine.NewPage();
        engine.Reset();
        Assert.Equal(0f, engine.CursorY);
        Assert.Equal(1, engine.CurrentPage);
    }

    [Fact]
    public void AdvanceTo_SetsCursorDirectly()
    {
        var engine = CreateEngine();
        engine.AdvanceTo(150f);
        Assert.Equal(150f, engine.CursorY, 0.001f);
    }

    [Fact]
    public void ContentLeft_EqualsLeftMargin()
    {
        var engine = CreateEngine();
        Assert.Equal(36f, engine.ContentLeft, 0.001f);
    }

    [Fact]
    public void ContentRight_EqualsPageWidthMinusRightMargin()
    {
        var engine = CreateEngine();
        Assert.Equal(595f - 36f, engine.ContentRight, 0.001f);
    }
}
