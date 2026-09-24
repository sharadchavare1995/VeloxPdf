namespace VeloxPdf.Tests.Layout;
using VeloxPdf.Fonts;
using VeloxPdf.Layout;
using Xunit;

public class TextWrapperTests
{
    private static readonly Type1Font Font = Type1Font.Helvetica;

    [Fact]
    public void ShortText_FitsOnOneLine()
    {
        var lines = TextWrapper.WrapText("Hi", 500f, Font, 12f);
        Assert.Single(lines);
        Assert.Equal("Hi", lines[0].Content);
        Assert.True(lines[0].IsLastInParagraph);
    }

    [Fact]
    public void EmptyText_ReturnsOneEmptyLine()
    {
        var lines = TextWrapper.WrapText("", 500f, Font, 12f);
        Assert.Single(lines);
        Assert.Equal("", lines[0].Content);
    }

    [Fact]
    public void LongText_WrapsToMultipleLines()
    {
        string longText = string.Join(" ", Enumerable.Repeat("word", 30));
        var lines = TextWrapper.WrapText(longText, 200f, Font, 10f);
        Assert.True(lines.Count > 1);
    }

    [Fact]
    public void ExplicitNewline_CreatesNewLine()
    {
        var lines = TextWrapper.WrapText("Line one\nLine two", 500f, Font, 12f);
        Assert.True(lines.Count >= 2);
        Assert.Equal("Line one", lines[0].Content);
        Assert.Equal("Line two", lines[1].Content);
    }

    [Fact]
    public void LastLineIsMarkedAsLast()
    {
        string text = "First line\nSecond line";
        var lines = TextWrapper.WrapText(text, 500f, Font, 12f);
        Assert.True(lines[^1].IsLastInParagraph);
    }

    [Fact]
    public void NonLastLine_NotMarkedAsLast()
    {
        string longText = string.Join(" ", Enumerable.Repeat("word", 50));
        var lines = TextWrapper.WrapText(longText, 100f, Font, 10f);
        Assert.True(lines.Count > 1);
        Assert.False(lines[0].IsLastInParagraph);
    }

    [Fact]
    public void VeryLongWord_ForcesBreak()
    {
        string longWord = new string('A', 100);
        var lines = TextWrapper.WrapText(longWord, 200f, Font, 12f);
        Assert.True(lines.Count > 1);
        int totalChars = lines.Sum(l => l.Content.Length);
        Assert.Equal(longWord.Length, totalChars);
    }

    [Fact]
    public void LineWidths_DoNotExceedMaxWidth()
    {
        string text = string.Join(" ", Enumerable.Repeat("Hello", 20));
        float maxWidth = 150f;
        var lines = TextWrapper.WrapText(text, maxWidth, Font, 10f);
        foreach (var line in lines)
        {
            Assert.True(line.Width <= maxWidth + 1f,
                $"Line '{line.Content}' width {line.Width} exceeds max {maxWidth}");
        }
    }

    [Fact]
    public void SingleWord_NotWrapped()
    {
        var lines = TextWrapper.WrapText("Hello", 500f, Font, 12f);
        Assert.Single(lines);
        Assert.Equal("Hello", lines[0].Content);
    }

    [Fact]
    public void MultipleNewlines_CreateMultipleEmptyLines()
    {
        var lines = TextWrapper.WrapText("A\n\nB", 500f, Font, 12f);
        Assert.True(lines.Count >= 3);
    }
}
