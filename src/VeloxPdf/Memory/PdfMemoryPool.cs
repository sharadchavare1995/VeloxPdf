namespace VeloxPdf.Memory;
using System.Buffers;
using VeloxPdf.IO;

/// <summary>
/// Central pooling hub for the VeloxPdf library.
/// All hot-path allocations should go through here to minimise GC pressure.
/// </summary>
public static class PdfMemoryPool
{
    // Byte-array pool (ArrayPool is thread-safe)
    public static ArrayPool<byte> Bytes => ArrayPool<byte>.Shared;

    // StringBuilder pool implemented with a simple concurrent bag
    private static readonly System.Collections.Concurrent.ConcurrentBag<StringBuilder> _sbPool = new();
    private const int MaxPooledSbCapacity = 65536;

    public static StringBuilder RentStringBuilder(int initialCapacity = 4096)
    {
        if (_sbPool.TryTake(out StringBuilder? sb))
        {
            sb.Clear();
            return sb;
        }
        return new StringBuilder(initialCapacity);
    }

    public static void ReturnStringBuilder(StringBuilder sb)
    {
        if (sb.Capacity <= MaxPooledSbCapacity)
        {
            sb.Clear();
            _sbPool.Add(sb);
        }
        // If oversized, let GC collect it
    }

    public static byte[] RentBytes(int minimumLength)
        => Bytes.Rent(minimumLength);

    public static void ReturnBytes(byte[] buffer)
        => Bytes.Return(buffer);

    /// <summary>
    /// Rent a PooledMemoryStream sized to the nearest power-of-two tier
    /// (16 KB, 64 KB, 256 KB, 1 MB) to avoid buffer resizing in most cases.
    /// </summary>
    public static PooledMemoryStream RentStream(int estimatedSize)
    {
        int actualSize = estimatedSize <= 16 * 1024  ? 16 * 1024
                       : estimatedSize <= 64 * 1024  ? 64 * 1024
                       : estimatedSize <= 256 * 1024 ? 256 * 1024
                       : 1024 * 1024;
        return new PooledMemoryStream(actualSize);
    }

    /// <summary>
    /// Return a PooledMemoryStream to the pool.  Calling Reset() prepares it for reuse;
    /// the underlying rented buffer is reclaimed when the stream is next disposed or collected.
    /// </summary>
    public static void ReturnStream(PooledMemoryStream stream)
    {
        stream.Reset();
    }
}
