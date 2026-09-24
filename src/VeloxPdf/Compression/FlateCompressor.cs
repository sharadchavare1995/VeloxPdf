namespace VeloxPdf.Compression;
using VeloxPdf.IO;

/// <summary>
/// Compresses PDF stream data using zlib (ZLibStream), which produces the
/// two-byte zlib header and Adler-32 checksum required by PDF's FlateDecode filter.
/// </summary>
public sealed class FlateCompressor : ICompressor
{
    private readonly CompressionLevel _level;

    public FlateCompressor(CompressionLevel level = CompressionLevel.Optimal)
    {
        _level = level;
    }

    /// <summary>Singleton with default (Optimal) compression level.</summary>
    public static readonly FlateCompressor Default = new();

    /// <summary>Instance with fastest compression for interactive paths.</summary>
    public static readonly FlateCompressor Fastest = new(CompressionLevel.Fastest);

    /// <summary>Instance with no compression (still wraps in zlib framing).</summary>
    public static readonly FlateCompressor NoCompression = new(CompressionLevel.NoCompression);

    public string? FilterName => "FlateDecode";

    public byte[] Compress(ReadOnlySpan<byte> data)
    {
        // ZLibStream produces the correct zlib wrapper (RFC 1950) that PDF readers expect.
        // DeflateStream alone produces raw DEFLATE (RFC 1951) without the zlib header/checksum,
        // which violates the FlateDecode spec and causes reader failures.
        //
        // PooledMemoryStream uses ArrayPool<byte> for its backing store so the write buffer
        // never hits the GC heap — eliminating the MemoryStream internal-buffer allocation
        // that new MemoryStream(data.Length) would otherwise create per page.
        // +64 bytes covers zlib framing (2-byte header + 4-byte Adler-32 + block headers).
        using var ms = new PooledMemoryStream(data.Length + 64);
        using (var zl = new ZLibStream(ms, _level, leaveOpen: true))
            zl.Write(data);
        return ms.ToArray();
    }
}
