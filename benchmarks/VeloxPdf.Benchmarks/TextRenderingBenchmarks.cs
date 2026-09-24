namespace VeloxPdf.Benchmarks;
using BenchmarkDotNet.Attributes;
using VeloxPdf.Document;
using VeloxPdf.Elements;
using VeloxPdf.Fonts;
using VeloxPdf.Layout;
using VeloxPdf.Styling;

[MemoryDiagnoser]
public class TextRenderingBenchmarks
{
    private PdfDocument _doc = null!;

    [GlobalSetup]
    public void Setup()
    {
        _doc = new PdfDocument();
    }

    [Benchmark]
    public byte[] Generate_SinglePage_Text()
    {
        var doc = new PdfDocument();
        doc.Elements.Add(new TextElement { Text = "Hello, World! This is a benchmark test." });
        return doc.Save();
    }

    [Benchmark]
    public byte[] Generate_Heading_And_Paragraphs()
    {
        var doc = new PdfDocument();
        doc.Elements.Add(new TextElement { Text = "Report Title", Style = TextStyle.Heading1 });
        for (int i = 0; i < 10; i++)
            doc.Elements.Add(new TextElement
            {
                Text = $"Paragraph {i + 1}: Lorem ipsum dolor sit amet, consectetur adipiscing elit. " +
                       "Sed do eiusmod tempor incididunt ut labore et dolore magna aliqua."
            });
        return doc.Save();
    }

    [Benchmark]
    public float[] WrapText_10000Lines()
    {
        var font = Type1Font.Helvetica;
        float maxWidth = 500f;
        string text = string.Join(" ", Enumerable.Repeat("Hello World", 1000));
        var lines = TextWrapper.WrapText(text, maxWidth, font, 10f);
        return lines.Select(l => l.Width).ToArray();
    }

    [Benchmark]
    public float MeasureText_Performance()
    {
        var font = Type1Font.Helvetica;
        float total = 0;
        string text = "The quick brown fox jumps over the lazy dog";
        for (int i = 0; i < 10000; i++)
            total += font.MeasureWidth(text.AsSpan(), 10f);
        return total;
    }
}
