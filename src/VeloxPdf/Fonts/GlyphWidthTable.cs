namespace VeloxPdf.Fonts;

/// <summary>Thread-safe cache of character-code to glyph-width mappings.</summary>
public sealed class GlyphWidthTable : IDisposable
{
    private readonly Dictionary<int, float> _widths = new();
    private readonly ReaderWriterLockSlim _lock = new(LockRecursionPolicy.NoRecursion);
    private bool _disposed;

    /// <summary>Set the width for a single character code.</summary>
    public void Set(int charCode, float width)
    {
        _lock.EnterWriteLock();
        try { _widths[charCode] = width; }
        finally { _lock.ExitWriteLock(); }
    }

    /// <summary>Get the width for a character code, returning <paramref name="defaultWidth"/> if not found.</summary>
    public float Get(int charCode, float defaultWidth = 0f)
    {
        _lock.EnterReadLock();
        try { return _widths.TryGetValue(charCode, out float w) ? w : defaultWidth; }
        finally { _lock.ExitReadLock(); }
    }

    /// <summary>Try to get the width for a character code.</summary>
    public bool TryGet(int charCode, out float width)
    {
        _lock.EnterReadLock();
        try { return _widths.TryGetValue(charCode, out width); }
        finally { _lock.ExitReadLock(); }
    }

    /// <summary>Bulk-load widths from an array, starting at <paramref name="firstChar"/>.</summary>
    public void LoadFromArray(float[] widths, int firstChar = 0)
    {
        _lock.EnterWriteLock();
        try
        {
            for (int i = 0; i < widths.Length; i++)
                _widths[firstChar + i] = widths[i];
        }
        finally { _lock.ExitWriteLock(); }
    }

    /// <summary>Number of entries in the table.</summary>
    public int Count
    {
        get
        {
            _lock.EnterReadLock();
            try { return _widths.Count; }
            finally { _lock.ExitReadLock(); }
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _lock.Dispose();
            _disposed = true;
        }
    }
}
