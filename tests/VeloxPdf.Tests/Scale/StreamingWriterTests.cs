namespace VeloxPdf.Tests.Scale;
using VeloxPdf.Document;
using VeloxPdf.Streaming;
using Xunit;

public class StreamingWriterTests
{
    [Fact]
    public async Task Write_1000Rows_ProducesValidPdf()
    {
        using var ms = new MemoryStream();
        var schema = new StreamingTableSchema
        {
            ColumnHeaders = ["ID", "Name", "Value"],
            PageSize = PdfPageSize.A4
        };
        var options = new StreamingPdfOptions { RowsPerPage = 50 };

        await using var writer = new StreamingPdfWriter(ms, options);
        await writer.BeginDocumentAsync(new PdfMetadata { Title = "Test" }, schema);

        async IAsyncEnumerable<string[]> GenerateRows()
        {
            for (int i = 1; i <= 1000; i++)
                yield return [$"{i}", $"Item {i}", $"{i * 10}"];
        }

        await writer.WriteRowsAsync(GenerateRows());
        await writer.EndDocumentAsync();

        byte[] bytes = ms.ToArray();
        Assert.True(bytes.Length > 0);
        string header = System.Text.Encoding.ASCII.GetString(bytes, 0, 5);
        Assert.Equal("%PDF-", header);
        string content = System.Text.Encoding.GetEncoding("iso-8859-1").GetString(bytes);
        Assert.Contains("%%EOF", content);
    }

    [Fact]
    public async Task Write_Enumerable_Rows_Works()
    {
        using var ms = new MemoryStream();
        var schema = new StreamingTableSchema
        {
            ColumnHeaders = ["ID", "Name"]
        };
        var options = new StreamingPdfOptions { RowsPerPage = 10 };

        await using var writer = new StreamingPdfWriter(ms, options);
        await writer.BeginDocumentAsync(new PdfMetadata(), schema);

        var rows = Enumerable.Range(1, 100).Select(i => new[] { $"{i}", $"Name {i}" });
        await writer.WriteRowsAsync(rows);
        await writer.EndDocumentAsync();

        byte[] bytes = ms.ToArray();
        Assert.True(bytes.Length > 0);
        string header = System.Text.Encoding.ASCII.GetString(bytes, 0, 5);
        Assert.Equal("%PDF-", header);
    }

    [Fact]
    public async Task ProgressCallback_IsCalled()
    {
        using var ms = new MemoryStream();
        var progressCalls = 0;
        var options = new StreamingPdfOptions
        {
            RowsPerPage = 5,
            ChunkSize = 10,
            OnProgress = _ => Interlocked.Increment(ref progressCalls)
        };
        var schema = new StreamingTableSchema { ColumnHeaders = ["A"] };

        await using var writer = new StreamingPdfWriter(ms, options);
        await writer.BeginDocumentAsync(new PdfMetadata(), schema);
        var rows = Enumerable.Range(1, 50).Select(i => new[] { $"{i}" });
        await writer.WriteRowsAsync(rows);
        await writer.EndDocumentAsync();

        Assert.True(progressCalls > 0);
    }

    [Fact]
    public async Task CancellationToken_StopsGeneration()
    {
        using var ms = new MemoryStream();
        var cts = new CancellationTokenSource();
        var schema = new StreamingTableSchema { ColumnHeaders = ["ID"] };
        var options = new StreamingPdfOptions { RowsPerPage = 100 };

        await using var writer = new StreamingPdfWriter(ms, options);
        await writer.BeginDocumentAsync(new PdfMetadata(), schema);

        async IAsyncEnumerable<string[]> InfiniteRows(
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
        {
            int i = 0;
            while (true)
            {
                ct.ThrowIfCancellationRequested();
                yield return new string[] { $"{i++}" };
                if (i == 500) cts.Cancel();
                if (i % 100 == 0) await Task.Yield();
            }
        }

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await writer.WriteRowsAsync(InfiniteRows(cts.Token), cts.Token);
        });
    }

    [Fact]
    public async Task MemoryUsage_StaysReasonable_For10K_Rows()
    {
        using var ms = new MemoryStream();
        GC.Collect(2, GCCollectionMode.Forced, blocking: true);
        long memBefore = GC.GetTotalMemory(true);

        var schema = new StreamingTableSchema
        {
            ColumnHeaders = ["ID", "Name", "Dept", "Salary"]
        };
        var options = new StreamingPdfOptions { RowsPerPage = 50 };

        await using var writer = new StreamingPdfWriter(ms, options);
        await writer.BeginDocumentAsync(new PdfMetadata(), schema);
        var rows = Enumerable.Range(1, 10_000)
            .Select(i => new[] { $"{i}", $"Employee {i}", "Engineering", "$75000" });
        await writer.WriteRowsAsync(rows);
        await writer.EndDocumentAsync();

        long memAfter = GC.GetTotalMemory(false);
        long memUsedMB = (memAfter - memBefore) / 1024 / 1024;

        Assert.True(memUsedMB < 200,
            $"Memory usage too high: {memUsedMB}MB (expected < 200MB)");

        byte[] bytes = ms.ToArray();
        Assert.True(bytes.Length > 0);
    }

    [Fact]
    public async Task EmptyRows_ProducesValidPdf()
    {
        using var ms = new MemoryStream();
        var schema = new StreamingTableSchema { ColumnHeaders = ["A", "B"] };
        var options = new StreamingPdfOptions();

        await using var writer = new StreamingPdfWriter(ms, options);
        await writer.BeginDocumentAsync(new PdfMetadata { Title = "Empty" }, schema);
        await writer.WriteRowsAsync(Array.Empty<string[]>());
        await writer.EndDocumentAsync();

        byte[] bytes = ms.ToArray();
        Assert.True(bytes.Length > 0);
        string header = System.Text.Encoding.ASCII.GetString(bytes, 0, 5);
        Assert.Equal("%PDF-", header);
    }

    [Fact]
    public async Task Write_WithProjection_Works()
    {
        using var ms = new MemoryStream();
        var schema = new StreamingTableSchema { ColumnHeaders = ["ID", "Name"] };
        var options = new StreamingPdfOptions { RowsPerPage = 20 };

        await using var writer = new StreamingPdfWriter(ms, options);
        await writer.BeginDocumentAsync(new PdfMetadata(), schema);

        var records = Enumerable.Range(1, 50).Select(i => (Id: i, Name: $"Item {i}"));

        async IAsyncEnumerable<(int Id, string Name)> ToAsync()
        {
            foreach (var r in records) { yield return r; await Task.Yield(); }
        }

        await writer.WriteRowsAsync(ToAsync(), r => new[] { $"{r.Id}", r.Name });
        await writer.EndDocumentAsync();

        byte[] bytes = ms.ToArray();
        Assert.True(bytes.Length > 0);
    }
}
