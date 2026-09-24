namespace VeloxPdf.Compression;

/// <summary>
/// Abstraction for PDF stream compression.
/// Implementations should be stateless and thread-safe.
/// </summary>
public interface ICompressor
{
    /// <summary>
    /// Compresses the given data and returns the compressed bytes.
    /// </summary>
    byte[] Compress(ReadOnlySpan<byte> data);

    /// <summary>
    /// The PDF filter name to put in the stream dictionary's /Filter entry,
    /// or <c>null</c> if no filter should be applied.
    /// </summary>
    string? FilterName { get; }
}
