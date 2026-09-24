namespace VeloxPdf.Benchmarks;
using BenchmarkDotNet.Attributes;
using VeloxPdf.Document;
using VeloxPdf.Elements;

[MemoryDiagnoser]
public class TableRenderingBenchmarks
{
    private List<string[]> _rows100 = null!;
    private List<string[]> _rows1000 = null!;

    [GlobalSetup]
    public void Setup()
    {
        _rows100 = Enumerable.Range(1, 100)
            .Select(i => new[] { $"{i}", $"Name {i}", $"Dept {i % 5}", $"${i * 1000}" })
            .ToList();
        _rows1000 = Enumerable.Range(1, 1000)
            .Select(i => new[] { $"{i}", $"Name {i}", $"Dept {i % 5}", $"${i * 1000}" })
            .ToList();
    }

    [Benchmark]
    public byte[] Table_100Rows()
    {
        var doc = new PdfDocument();
        doc.Elements.Add(new TableElement
        {
            Headers = ["ID", "Name", "Department", "Salary"],
            Rows = _rows100
        });
        return doc.Save();
    }

    [Benchmark]
    public byte[] Table_1000Rows()
    {
        var doc = new PdfDocument();
        doc.Elements.Add(new TableElement
        {
            Headers = ["ID", "Name", "Department", "Salary"],
            Rows = _rows1000
        });
        return doc.Save();
    }
}
