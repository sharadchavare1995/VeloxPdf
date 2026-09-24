namespace VeloxPdf.Streaming;

public sealed class StreamingProgress
{
    public long RowsProcessed { get; init; }
    public long TotalRows { get; init; }
    public int PagesGenerated { get; init; }
    public double PercentComplete => TotalRows > 0
        ? (double)RowsProcessed / TotalRows * 100 : 0;
    public TimeSpan Elapsed { get; init; }
    public TimeSpan? EstimatedRemaining { get; init; }
    public double RowsPerSecond => Elapsed.TotalSeconds > 0
        ? RowsProcessed / Elapsed.TotalSeconds : 0;
    public long MemoryUsedBytes { get; init; }

    public override string ToString() =>
        $"{RowsProcessed:N0}/{TotalRows:N0} rows | " +
        $"{PagesGenerated:N0} pages | " +
        $"{RowsPerSecond:N0} rows/s | " +
        $"{MemoryUsedBytes / 1024 / 1024:N0} MB RAM";
}
