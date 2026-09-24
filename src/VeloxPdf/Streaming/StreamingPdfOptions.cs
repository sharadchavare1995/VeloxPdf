namespace VeloxPdf.Streaming;

public sealed class StreamingPdfOptions
{
    public int RowsPerPage { get; set; } = 50;
    public int PageBufferCount { get; set; } = 2;
    public bool CompressPages { get; set; } = true;
    public int ChunkSize { get; set; } = 500;
    public int MaxDegreeOfParallelism { get; set; } = Environment.ProcessorCount;
    public bool UseAsyncIO { get; set; } = true;
    public long EstimatedTotalRows { get; set; } = 0;
    public Action<StreamingProgress>? OnProgress { get; set; }
    public int CheckpointEveryNPages { get; set; } = 1000;
}
