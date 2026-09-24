namespace VeloxPdf.Tests.Primitives;
using VeloxPdf.Compression;
using VeloxPdf.IO;
using VeloxPdf.Primitives;
using Xunit;

public class PdfStreamTests
{
    [Fact]
    public void SetTextData_NoCompression_ContainsStreamKeyword()
    {
        var stream = new PdfStream();
        stream.ObjectNumber = 1;
        stream.SetTextData("Hello PDF", new NoCompressor());
        using var ms = new MemoryStream();
        using var writer = new PdfWriter(ms);
        stream.WriteTo(writer);
        string output = System.Text.Encoding.GetEncoding("iso-8859-1").GetString(ms.ToArray());
        Assert.Contains("stream", output);
        Assert.Contains("endstream", output);
        Assert.Contains("/Length", output);
    }

    [Fact]
    public void SetRawData_WithFlate_SetsFlateDecode()
    {
        var stream = new PdfStream();
        stream.ObjectNumber = 1;
        byte[] data = System.Text.Encoding.UTF8.GetBytes("Test content for compression");
        stream.SetRawData(data, new FlateCompressor());
        using var ms = new MemoryStream();
        using var writer = new PdfWriter(ms);
        stream.WriteTo(writer);
        string output = System.Text.Encoding.GetEncoding("iso-8859-1").GetString(ms.ToArray());
        Assert.Contains("FlateDecode", output);
    }

    [Fact]
    public void SetRawData_NoCompressor_NoFilterEntry()
    {
        var stream = new PdfStream();
        stream.ObjectNumber = 1;
        stream.SetRawData(new byte[] { 1, 2, 3 }, new NoCompressor());
        bool hasFilter = stream.Dictionary.ContainsKey("Filter");
        Assert.False(hasFilter);
    }

    [Fact]
    public void Length_ReflectsEncodedDataSize()
    {
        var stream = new PdfStream();
        stream.ObjectNumber = 1;
        string text = "BT /F1 12 Tf 100 700 Td (Hello) Tj ET";
        stream.SetTextData(text, new NoCompressor());
        Assert.Equal(
            System.Text.Encoding.GetEncoding("iso-8859-1").GetByteCount(text),
            stream.Length);
    }

    [Fact]
    public void SetRawData_SetsLengthInDictionary()
    {
        var stream = new PdfStream();
        stream.ObjectNumber = 1;
        byte[] data = new byte[100];
        stream.SetRawData(data, new NoCompressor());
        bool found = stream.Dictionary.TryGet<PdfInteger>("Length", out var lenObj);
        Assert.True(found);
        Assert.Equal(100, lenObj!.Value);
    }
}
