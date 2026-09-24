namespace VeloxPdf.Benchmarks;
using BenchmarkDotNet.Attributes;
using VeloxPdf.Document;
using VeloxPdf.Streaming;

[MemoryDiagnoser]
[GcForce(true)]
public class ScaleBenchmarks
{
    private static readonly string[] Departments = ["Engineering", "Sales", "Finance", "HR", "Ops"];
    private static readonly string[] Statuses = ["Active", "Inactive", "On Leave"];

    private static IEnumerable<string[]> GenerateRows(int count)
    {
        for (int i = 1; i <= count; i++)
        {
            yield return
            [
                i.ToString(),
                $"Employee {i}",
                Departments[i % Departments.Length],
                $"${45000 + (i % 80000)}",
                DateTime.Today.AddDays(-(i % 3650)).ToString("yyyy-MM-dd"),
                Statuses[i % Statuses.Length]
            ];
        }
    }

    private StreamingTableSchema CreateSchema() => new()
    {
        ColumnHeaders = ["ID", "Name", "Department", "Salary", "Join Date", "Status"],
        PageSize = PdfPageSize.A4,
        RepeatColumnHeadersOnEveryPage = true
    };

    [Benchmark]
    public async Task Generate_10K_Rows()
    {
        await using var writer = new StreamingPdfWriter(Stream.Null,
            new StreamingPdfOptions { RowsPerPage = 50 });
        await writer.BeginDocumentAsync(new PdfMetadata { Title = "10K Rows" }, CreateSchema());
        await writer.WriteRowsAsync(GenerateRows(10_000));
        await writer.EndDocumentAsync();
    }

    [Benchmark]
    public async Task Generate_100K_Rows()
    {
        await using var writer = new StreamingPdfWriter(Stream.Null,
            new StreamingPdfOptions { RowsPerPage = 50 });
        await writer.BeginDocumentAsync(new PdfMetadata { Title = "100K Rows" }, CreateSchema());
        await writer.WriteRowsAsync(GenerateRows(100_000));
        await writer.EndDocumentAsync();
    }

    [Benchmark]
    public async Task Generate_500K_Rows()
    {
        await using var writer = new StreamingPdfWriter(Stream.Null,
            new StreamingPdfOptions { RowsPerPage = 50 });
        await writer.BeginDocumentAsync(new PdfMetadata { Title = "500K Rows" }, CreateSchema());
        await writer.WriteRowsAsync(GenerateRows(500_000));
        await writer.EndDocumentAsync();
    }

    [Benchmark]
    public async Task Generate_2M_Rows()
    {
        await using var writer = new StreamingPdfWriter(Stream.Null,
            new StreamingPdfOptions { RowsPerPage = 50, ChunkSize = 1000 });
        await writer.BeginDocumentAsync(new PdfMetadata { Title = "2M Rows" }, CreateSchema());
        await writer.WriteRowsAsync(GenerateRows(2_000_000));
        await writer.EndDocumentAsync();
    }
}
