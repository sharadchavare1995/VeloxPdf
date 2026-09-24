namespace VeloxPdf.IO;

using System.Buffers;

/// <summary>
/// An IBufferWriter&lt;byte&gt; backed by a pooled byte array.
/// Avoids allocations in high-throughput rendering paths.
/// </summary>
public sealed class PdfBufferWriter : IBufferWriter<byte>, IDisposable
{
    private byte[] _buffer;
    private int    _written;
    private bool   _disposed;

    private const int DefaultCapacity = 65536; // 64 KB

    public PdfBufferWriter(int initialCapacity = DefaultCapacity)
    {
        _buffer  = ArrayPool<byte>.Shared.Rent(Math.Max(initialCapacity, 64));
        _written = 0;
    }

    /// <summary>Total bytes written since last Reset().</summary>
    public int WrittenCount => _written;

    // ── IBufferWriter<byte> ──────────────────────────────────────────────────

    public void Advance(int count)
    {
        if (count < 0 || _written + count > _buffer.Length)
            throw new ArgumentOutOfRangeException(nameof(count));
        _written += count;
    }

    public Memory<byte> GetMemory(int sizeHint = 0)
    {
        EnsureCapacity(sizeHint > 0 ? sizeHint : 1);
        return _buffer.AsMemory(_written);
    }

    public Span<byte> GetSpan(int sizeHint = 0)
    {
        EnsureCapacity(sizeHint > 0 ? sizeHint : 1);
        return _buffer.AsSpan(_written);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    public ReadOnlySpan<byte>   GetWrittenSpan()   => _buffer.AsSpan(0, _written);
    public ReadOnlyMemory<byte> GetWrittenMemory() => _buffer.AsMemory(0, _written);

    /// <summary>Returns a new byte[] containing exactly the written bytes.</summary>
    public byte[] ToArray() => _buffer[.._written].ToArray();

    /// <summary>Resets the write cursor without releasing the buffer.</summary>
    public void Reset() => _written = 0;

    /// <summary>Write bytes directly into the buffer.</summary>
    public void Write(ReadOnlySpan<byte> data)
    {
        EnsureCapacity(data.Length);
        data.CopyTo(_buffer.AsSpan(_written));
        _written += data.Length;
    }

    /// <summary>Write a byte array directly into the buffer.</summary>
    public void Write(byte[] data) => Write(data.AsSpan());

    // ── Capacity management ──────────────────────────────────────────────────

    public void EnsureCapacity(int needed)
    {
        if (_written + needed <= _buffer.Length) return;

        int newCapacity = Math.Max(_buffer.Length * 2, _written + needed);
        byte[] newBuffer = ArrayPool<byte>.Shared.Rent(newCapacity);
        _buffer.AsSpan(0, _written).CopyTo(newBuffer);
        ArrayPool<byte>.Shared.Return(_buffer);
        _buffer = newBuffer;
    }

    // ── IDisposable ──────────────────────────────────────────────────────────

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            ArrayPool<byte>.Shared.Return(_buffer);
            _buffer = Array.Empty<byte>();
        }
    }
}
