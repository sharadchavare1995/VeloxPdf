namespace VeloxPdf.Tests.Primitives;
using VeloxPdf.IO;
using VeloxPdf.Primitives;
using Xunit;

public class PdfDictionaryTests
{
    private static string Serialize(PdfObject obj)
    {
        using var ms = new MemoryStream();
        using var writer = new PdfWriter(ms);
        obj.WriteTo(writer);
        return System.Text.Encoding.GetEncoding("iso-8859-1").GetString(ms.ToArray());
    }

    [Fact]
    public void EmptyDictionary_WritesEmptyBraces()
    {
        var dict = new PdfDictionary();
        string output = Serialize(dict);
        Assert.Equal("<<\n>>", output);
    }

    [Fact]
    public void SetInt_ProducesCorrectSyntax()
    {
        var dict = new PdfDictionary();
        dict.Set("Count", 5);
        string output = Serialize(dict);
        Assert.Contains("/Count 5", output);
    }

    [Fact]
    public void SetFloat_UsesInvariantCulture()
    {
        var dict = new PdfDictionary();
        dict.Set("Width", 595.28f);
        string output = Serialize(dict);
        Assert.Contains("/Width 595.28", output);
        Assert.DoesNotContain(",", output);
    }

    [Fact]
    public void SetBool_WritesTrueOrFalse()
    {
        var dict = new PdfDictionary();
        dict.Set("Hidden", true);
        string output = Serialize(dict);
        Assert.Contains("/Hidden true", output);
    }

    [Fact]
    public void TryGet_ReturnsCorrectType()
    {
        var dict = new PdfDictionary();
        dict.Set("Type", "Font");
        bool found = dict.TryGet<PdfName>("Type", out var name);
        Assert.True(found);
        Assert.NotNull(name);
        Assert.Equal("Font", name!.Name);
    }

    [Fact]
    public void Remove_DeletesKey()
    {
        var dict = new PdfDictionary();
        dict.Set("Temp", 42);
        dict.Remove("Temp");
        bool found = dict.TryGet<PdfInteger>("Temp", out _);
        Assert.False(found);
    }

    [Fact]
    public void FluentSet_ReturnsThis()
    {
        var dict = new PdfDictionary();
        var result = dict.Set("A", 1).Set("B", 2f).Set("C", true);
        Assert.Same(dict, result);
        Assert.Equal(3, dict.Count);
    }

    [Fact]
    public void MultipleEntries_WritesAllEntries()
    {
        var dict = new PdfDictionary();
        dict.Set("Type", "Page");
        dict.Set("Width", 595f);
        dict.Set("Count", 10);
        string output = Serialize(dict);
        Assert.Contains("/Type /Page", output);
        Assert.Contains("/Width 595", output);
        Assert.Contains("/Count 10", output);
    }
}
