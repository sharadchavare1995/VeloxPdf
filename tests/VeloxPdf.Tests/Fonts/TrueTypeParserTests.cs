namespace VeloxPdf.Tests.Fonts;
using VeloxPdf.Fonts;
using Xunit;

public class TrueTypeParserTests
{
    [Fact]
    public void GlyphWidthTable_StoresAndRetrieves()
    {
        var table = new GlyphWidthTable();
        table.Set(65, 700f);
        bool found = table.TryGet(65, out float width);
        Assert.True(found);
        Assert.Equal(700f, width);
    }

    [Fact]
    public void GlyphWidthTable_ReturnsFalseForMissingChar()
    {
        var table = new GlyphWidthTable();
        bool found = table.TryGet(999, out float width);
        Assert.False(found);
        Assert.Equal(0f, width);
    }

    [Fact]
    public void GlyphWidthTable_LoadFromArray()
    {
        var table = new GlyphWidthTable();
        float[] widths = [300f, 400f, 500f];
        table.LoadFromArray(widths, firstChar: 32);
        Assert.Equal(300f, table.Get(32));
        Assert.Equal(400f, table.Get(33));
        Assert.Equal(500f, table.Get(34));
    }

    [Fact]
    public void GlyphWidthTable_DefaultForMissing()
    {
        var table = new GlyphWidthTable();
        float w = table.Get(99, defaultWidth: 600f);
        Assert.Equal(600f, w);
    }

    [Fact]
    public void GlyphWidthTable_IsThreadSafe()
    {
        var table = new GlyphWidthTable();
        var tasks = Enumerable.Range(0, 10).Select(i =>
            Task.Run(() =>
            {
                table.Set(i, i * 100f);
                float _ = table.Get(i);
            })).ToArray();
        Assert.True(Task.WaitAll(tasks, timeout: TimeSpan.FromSeconds(5)));
    }

    [Fact]
    public void GlyphWidthTable_OverwritesPreviousValue()
    {
        var table = new GlyphWidthTable();
        table.Set(65, 500f);
        table.Set(65, 750f);
        float w = table.Get(65);
        Assert.Equal(750f, w);
    }

    [Fact]
    public void GlyphWidthTable_LoadFromArray_MultipleRanges()
    {
        var table = new GlyphWidthTable();
        table.LoadFromArray([100f, 200f], firstChar: 32);
        table.LoadFromArray([300f, 400f], firstChar: 65);
        Assert.Equal(100f, table.Get(32));
        Assert.Equal(300f, table.Get(65));
    }
}
