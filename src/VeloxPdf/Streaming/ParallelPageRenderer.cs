namespace VeloxPdf.Streaming;
using System.Threading.Channels;
using VeloxPdf.Compression;

/// <summary>
/// Producer-consumer parallel page renderer.
/// Workers render pages in parallel; a single writer outputs them in strict page-number order.
/// Uses bounded channels for backpressure to cap memory usage.
/// </summary>
public sealed class ParallelPageRenderer : IAsyncDisposable
{
    private readonly PageRenderPipeline _pipeline;
    private readonly Channel<PageRenderJob> _jobChannel;
    private readonly Channel<CompletedPage> _resultChannel;
    private readonly CancellationTokenSource _cts = new();
    private readonly List<Task> _workers = new();

    public ParallelPageRenderer(
        StreamingTableSchema schema,
        StreamingPdfOptions options,
        ICompressor compressor)
    {
        _pipeline = new PageRenderPipeline(schema, compressor);

        int parallelism = Math.Max(1, options.MaxDegreeOfParallelism);

        _jobChannel = Channel.CreateBounded<PageRenderJob>(
            new BoundedChannelOptions(parallelism * 2)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = false,
                SingleWriter = true
            });

        _resultChannel = Channel.CreateBounded<CompletedPage>(
            new BoundedChannelOptions(parallelism * 4)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = true,
                SingleWriter = false
            });

        // Start worker tasks
        for (int i = 0; i < parallelism; i++)
        {
            var worker = Task.Run(() => WorkerLoop(_cts.Token));
            _workers.Add(worker);
        }
    }

    private async Task WorkerLoop(CancellationToken ct)
    {
        await foreach (var job in _jobChannel.Reader.ReadAllAsync(ct))
        {
            ct.ThrowIfCancellationRequested();

            byte[] compressed = _pipeline.RenderPage(job, out _);

            var completed = new CompletedPage
            {
                PageNumber = job.PageNumber,
                PageObjectNumber = job.PageObjectNumber,
                ContentObjectNumber = job.ContentObjectNumber,
                CompressedContent = compressed,
                ContentLength = compressed.Length
            };

            await _resultChannel.Writer.WriteAsync(completed, ct);
        }
    }

    /// <summary>Submit a page render job to the worker pool.</summary>
    public async Task SubmitJobAsync(PageRenderJob job, CancellationToken ct)
        => await _jobChannel.Writer.WriteAsync(job, ct);

    /// <summary>Signal that no more jobs will be submitted.</summary>
    public void CompleteJobs() => _jobChannel.Writer.Complete();

    /// <summary>Enumerate completed pages as they finish (may arrive out of order).</summary>
    public async IAsyncEnumerable<CompletedPage> GetCompletedPagesAsync(
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        await foreach (var page in _resultChannel.Reader.ReadAllAsync(ct))
            yield return page;
    }

    /// <summary>Wait for all worker tasks to finish after CompleteJobs() is called.</summary>
    public async Task WaitForWorkersAsync()
    {
        await Task.WhenAll(_workers);
        _resultChannel.Writer.TryComplete();
    }

    public async ValueTask DisposeAsync()
    {
        _cts.Cancel();
        _jobChannel.Writer.TryComplete();
        _resultChannel.Writer.TryComplete();
        try { await Task.WhenAll(_workers); } catch (OperationCanceledException) { }
        _cts.Dispose();
    }
}
