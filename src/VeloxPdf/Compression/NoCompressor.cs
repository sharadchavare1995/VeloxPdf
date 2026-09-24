namespace VeloxPdf.Compression;

/// <summary>
/// Pass-through compressor — stores stream data without any compression.
/// No /Filter entry is written to the stream dictionary.
/// </summary>
public sealed class NoCompressor : ICompressor
{
    public static readonly NoCompressor Instance = new();

    public NoCompressor() { }

    /// <summary>Returns null so the stream dictionary has no /Filter entry.</summary>
    public string? FilterName => null;

    /// <summary>Returns a copy of the data unchanged.</summary>
    public byte[] Compress(ReadOnlySpan<byte> data) => data.ToArray();
}
