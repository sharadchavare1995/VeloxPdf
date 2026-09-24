namespace VeloxPdf.IO;

/// <summary>
/// A MemoryStream backed by a rented ArrayPool buffer. Returns the buffer
/// to the pool on Dispose, minimising GC pressure for high-throughput scenarios.
/// </summary>
public sealed class PooledMemoryStream : Stream
{
    private byte[] _buffer;
    private int    _length;    // total bytes written
    private int    _position;  // current read/write cursor
    private bool   _disposed;

    private const int DefaultCapacity = 65536; // 64 KB

    public PooledMemoryStream(int initialCapacity = DefaultCapacity)
    {
        _buffer = ArrayPool<byte>.Shared.Rent(Math.Max(initialCapacity, 16));
        _length   = 0;
        _position = 0;
    }

    // ── Stream property overrides ────────────────────────────────────────────

    public override bool CanRead  => !_disposed;
    public override bool CanWrite => !_disposed;
    public override bool CanSeek  => !_disposed;

    public override long Length
    {
        get { CheckDisposed(); return _length; }
    }

    public override long Position
    {
        get { CheckDisposed(); return _position; }
        set
        {
            CheckDisposed();
            if (value < 0 || value > _length)
                throw new ArgumentOutOfRangeException(nameof(value));
            _position = (int)value;
        }
    }

    // ── Write ────────────────────────────────────────────────────────────────

    public override void Write(byte[] buffer, int offset, int count)
        => Write(buffer.AsSpan(offset, count));

    public override void Write(ReadOnlySpan<byte> buffer)
    {
        CheckDisposed();
        EnsureCapacity(_position + buffer.Length);
        buffer.CopyTo(_buffer.AsSpan(_position));
        _position += buffer.Length;
        if (_position > _length) _length = _position;
    }

    public override void WriteByte(byte value)
    {
        CheckDisposed();
        EnsureCapacity(_position + 1);
        _buffer[_position++] = value;
        if (_position > _length) _length = _position;
    }

    // ── Read ─────────────────────────────────────────────────────────────────

    public override int Read(byte[] buffer, int offset, int count)
        => Read(buffer.AsSpan(offset, count));

    public override int Read(Span<byte> buffer)
    {
        CheckDisposed();
        int available = _length - _position;
        if (available <= 0) return 0;
        int n = Math.Min(available, buffer.Length);
        _buffer.AsSpan(_position, n).CopyTo(buffer);
        _position += n;
        return n;
    }

    public override int ReadByte()
    {
        CheckDisposed();
        if (_position >= _length) return -1;
        return _buffer[_position++];
    }

    // ── Seek ─────────────────────────────────────────────────────────────────

    public override long Seek(long offset, SeekOrigin origin)
    {
        CheckDisposed();
        long newPos = origin switch
        {
            SeekOrigin.Begin   => offset,
            SeekOrigin.Current => _position + offset,
            SeekOrigin.End     => _length + offset,
            _                  => throw new ArgumentException("Invalid SeekOrigin", nameof(origin))
        };
        if (newPos < 0) throw new IOException("Cannot seek before start of stream.");
        _position = (int)Math.Min(newPos, _length);
        return _position;
    }

    public override void SetLength(long value)
    {
        CheckDisposed();
        if (value < 0) throw new ArgumentOutOfRangeException(nameof(value));
        int len = (int)value;
        EnsureCapacity(len);
        if (len > _length)
            _buffer.AsSpan(_length, len - _length).Clear();
        _length = len;
        if (_position > _length) _position = _length;
    }

    public override void Flush() { /* no-op: in-memory */ }

    // ── Extra helpers ────────────────────────────────────────────────────────

    /// <summary>Returns a new byte[] containing exactly the written bytes.</summary>
    public byte[] ToArray() => _buffer[.._length].ToArray();

    /// <summary>Copies the written content to another stream.</summary>
    public void WriteTo(Stream destination)
    {
        CheckDisposed();
        destination.Write(_buffer, 0, _length);
    }

    /// <summary>Read-only view of the written bytes (no allocation).</summary>
    public ReadOnlySpan<byte> GetWrittenSpan()
    {
        CheckDisposed();
        return _buffer.AsSpan(0, _length);
    }

    public ReadOnlyMemory<byte> GetWrittenMemory()
    {
        CheckDisposed();
        return _buffer.AsMemory(0, _length);
    }

    /// <summary>
    /// Resets position and length to 0 so the buffer can be reused without
    /// returning it to the pool.
    /// </summary>
    public void Reset()
    {
        CheckDisposed();
        _position = 0;
        _length   = 0;
    }

    // ── Internal ─────────────────────────────────────────────────────────────

    private void EnsureCapacity(int required)
    {
        if (required <= _buffer.Length) return;

        int newCapacity = Math.Max(_buffer.Length * 2, required);
        byte[] newBuffer = ArrayPool<byte>.Shared.Rent(newCapacity);
        _buffer.AsSpan(0, _length).CopyTo(newBuffer);
        ArrayPool<byte>.Shared.Return(_buffer);
        _buffer = newBuffer;
    }

    private void CheckDisposed()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(PooledMemoryStream));
    }

    protected override void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            _disposed = true;
            ArrayPool<byte>.Shared.Return(_buffer);
            _buffer = Array.Empty<byte>();
        }
        base.Dispose(disposing);
    }
}
