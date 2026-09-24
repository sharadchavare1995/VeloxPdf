namespace VeloxPdf.Tests.Primitives;
using VeloxPdf.IO;
using VeloxPdf.Primitives;
using Xunit;

public class PdfArrayTests
{
    private static string Serialize(PdfObject obj)
    {
        using var ms = new MemoryStream();
        using var writer = new PdfWriter(ms);
        obj.WriteTo(writer);
        return System.Text.Encoding.GetEncoding("iso-8859-1").GetString(ms.ToArray());
    }

    [Fact]
    public void EmptyArray_WritesEmptyBrackets()
    {
        var arr = new PdfArray();
        string output = Serialize(arr);
        Assert.Equal("[ ]", output);
    }

    [Fact]
    public void IntegerElements_WritesCorrectly()
    {
        var arr = new PdfArray();
        arr.Add(0); arr.Add(0); arr.Add(595); arr.Add(842);
        string output = Serialize(arr);
        Assert.Equal("[ 0 0 595 842 ]", output);
    }

    [Fact]
    public void FloatElement_UsesInvariantCulture()
    {
        var arr = new PdfArray();
        arr.Add(3.14f);
        string output = Serialize(arr);
        Assert.Contains("3.14", output);
        Assert.DoesNotContain(",", output);
    }

    [Fact]
    public void NameElement_HasSlashPrefix()
    {
        var arr = new PdfArray();
        arr.Add("PDF");
        string output = Serialize(arr);
        Assert.Contains("/PDF", output);
    }

    [Fact]
    public void Indexer_ReturnsCorrectElement()
    {
        var arr = new PdfArray();
        arr.Add(new PdfInteger(42));
        var elem = arr[0] as PdfInteger;
        Assert.NotNull(elem);
        Assert.Equal(42, elem!.Value);
    }

    [Fact]
    public void Count_ReflectsItemCount()
    {
        var arr = new PdfArray();
        Assert.Equal(0, arr.Count);
        arr.Add(1); arr.Add(2); arr.Add(3);
        Assert.Equal(3, arr.Count);
    }
}
