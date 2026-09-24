namespace VeloxPdf.Samples;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using VeloxPdf.Document;
using VeloxPdf.Streaming;
using VeloxPdf.Styling;

public static class LargeScaleSample
{
    private static readonly string[] Departments =
        ["Engineering", "Sales", "Finance", "HR", "Operations", "Marketing", "Legal", "Support"];
    private static readonly string[] Statuses = ["Active", "Inactive", "On Leave", "Remote"];

    public static async Task Generate(int recordCount, string outputPath)
    {
        Console.WriteLine($"\nGenerating {recordCount:N0} records...");
        var sw = Stopwatch.StartNew();
        long peakMemory = 0;

        var schema = new StreamingTableSchema
        {
            ColumnHeaders = ["ID", "Name", "Department", "Salary", "Join Date", "Status"],
            PageSize = PdfPageSize.A4,
            Margins = PdfMargins.Default,
            RepeatColumnHeadersOnEveryPage = true,
            RepeatHeaderOnEveryPage = true,
            Style = new TableStyle
            {
                StripedRows = true,
                ShowBorder = true
            }
        };

        var options = new StreamingPdfOptions
        {
            RowsPerPage = 50,
            ChunkSize = 1000,
            CompressPages = true,
            EstimatedTotalRows = recordCount,
            OnProgress = p =>
            {
                long memMB = p.MemoryUsedBytes / 1024 / 1024;
                if (memMB > peakMemory) peakMemory = memMB;
                Console.Write(
                    $"\r  {p.RowsProcessed:N0}/{p.TotalRows:N0} rows " +
                    $"| {p.PagesGenerated:N0} pages " +
                    $"| {p.RowsPerSecond:N0} rows/s " +
                    $"| {memMB:N0} MB RAM " +
                    $"| {p.PercentComplete:F1}%    ");
            }
        };

        await using var fileStream = File.OpenWrite(outputPath);
        await using var writer = new StreamingPdfWriter(fileStream, options);

        await writer.BeginDocumentAsync(
            new PdfMetadata
            {
                Title = $"{recordCount:N0} Record Report",
                Author = "VeloxPdf LargeScaleSample",
                Creator = "VeloxPdf 1.0"
            },
            schema);

        await writer.WriteRowsAsync(GenerateRecordsAsync(recordCount));
        await writer.EndDocumentAsync();

        sw.Stop();
        Console.WriteLine();

        var fileInfo = new FileInfo(outputPath);
        double sizeMB = fileInfo.Length / 1024.0 / 1024.0;
        double rowsPerSec = recordCount / sw.Elapsed.TotalSeconds;

        Console.WriteLine($"  Complete!");
        Console.WriteLine($"  File:     {outputPath}");
        Console.WriteLine($"  Size:     {sizeMB:F2} MB");
        Console.WriteLine($"  Time:     {sw.Elapsed.TotalSeconds:F1}s");
        Console.WriteLine($"  Speed:    {rowsPerSec:N0} rows/sec");
        Console.WriteLine($"  Peak RAM: {peakMemory:N0} MB");
    }

    private static async IAsyncEnumerable<string[]> GenerateRecordsAsync(
        int count,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var random = new Random(42); // Deterministic for reproducibility

        for (int i = 1; i <= count; i++)
        {
            ct.ThrowIfCancellationRequested();

            string dept = Departments[i % Departments.Length];
            string status = Statuses[i % Statuses.Length];
            int salary = 45000 + random.Next(0, 80000);
            string joinDate = DateTime.Today.AddDays(-(i % 3650)).ToString("yyyy-MM-dd");

            yield return
            [
                i.ToString(),
                $"Employee {i:N0}",
                dept,
                $"${salary:N0}",
                joinDate,
                status
            ];

            // Simulate async I/O every 1000 rows (e.g. database batch reads)
            if (i % 1000 == 0)
                await Task.Yield();
        }
    }
}
