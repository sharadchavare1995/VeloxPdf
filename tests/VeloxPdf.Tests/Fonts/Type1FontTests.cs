namespace VeloxPdf.Tests.Fonts;
using VeloxPdf.Fonts;
using Xunit;

public class Type1FontTests
{
    [Fact]
    public void Helvetica_MeasuresSpaceWidth()
    {
        var font = Type1Font.Helvetica;
        float width = font.MeasureWidth(' ', 12f);
        Assert.InRange(width, 3f, 4f);
    }

    [Fact]
    public void Helvetica_MeasuresCapitalAWidth()
    {
        var font = Type1Font.Helvetica;
        float width = font.MeasureWidth('A', 12f);
        Assert.InRange(width, 7f, 10f);
    }

    [Fact]
    public void Courier_IsMonospaced()
    {
        var font = Type1Font.Courier;
        float widthA = font.MeasureWidth('A', 10f);
        float widthI = font.MeasureWidth('i', 10f);
        float widthW = font.MeasureWidth('W', 10f);
        Assert.Equal(widthA, widthI, 0.001f);
        Assert.Equal(widthA, widthW, 0.001f);
    }

    [Fact]
    public void MeasureTextWidth_SumsCharWidths()
    {
        var font = Type1Font.Helvetica;
        float hello = font.MeasureTextWidth("Hello", 10f);
        float h = font.MeasureWidth('H', 10f);
        float e = font.MeasureWidth('e', 10f);
        float l = font.MeasureWidth('l', 10f);
        float o = font.MeasureWidth('o', 10f);
        float expected = h + e + l + l + o;
        Assert.Equal(expected, hello, 0.001f);
    }

    [Fact]
    public void BuildFontDictionary_HasRequiredKeys()
    {
        var font = Type1Font.Helvetica;
        var dict = font.BuildFontDictionary();
        Assert.True(dict.ContainsKey("Type"));
        Assert.True(dict.ContainsKey("Subtype"));
        Assert.True(dict.ContainsKey("BaseFont"));
    }

    [Fact]
    public void LineHeight_IsGreaterThanFontSize()
    {
        var font = Type1Font.Helvetica;
        float lineH = font.LineHeight(12f);
        Assert.True(lineH > 12f);
    }

    [Fact]
    public void GetAdditionalObjects_ReturnsEmpty_ForBuiltInFonts()
    {
        var font = Type1Font.Helvetica;
        var extras = font.GetAdditionalObjects();
        Assert.Empty(extras);
    }

    [Fact]
    public void StaticFonts_HaveUniqueBaseFontNames()
    {
        var fonts = new[]
        {
            Type1Font.Helvetica, Type1Font.HelveticaBold, Type1Font.HelveticaOblique,
            Type1Font.TimesRoman, Type1Font.TimesBold, Type1Font.Courier, Type1Font.CourierBold
        };
        var names = fonts.Select(f => f.BaseFont).ToList();
        Assert.Equal(names.Count, names.Distinct().Count());
    }

    [Fact]
    public void ZeroFontSize_ReturnsZeroWidth()
    {
        var font = Type1Font.Helvetica;
        float width = font.MeasureWidth('A', 0f);
        Assert.Equal(0f, width, 0.001f);
    }

    [Fact]
    public void MeasureSpan_MatchesSumOfChars()
    {
        var font = Type1Font.Helvetica;
        const string text = "ABC";
        float spanWidth = font.MeasureWidth(text.AsSpan(), 10f);
        float sum = font.MeasureWidth('A', 10f) + font.MeasureWidth('B', 10f) + font.MeasureWidth('C', 10f);
        Assert.Equal(sum, spanWidth, 0.001f);
    }
}
