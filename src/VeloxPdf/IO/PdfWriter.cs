namespace VeloxPdf.IO;

using VeloxPdf.Primitives;

/// <summary>
/// Low-level PDF output writer. Wraps a Stream and provides PDF-specific
/// write helpers plus xref table management.
/// </summary>
public sealed class PdfWriter : IDisposable
{
    private readonly Stream _output;
    private int _objectCounter = 0;
    private readonly List<(int ObjNum, long Offset)> _xrefEntries;
    private static readonly Encoding _latin1 = Encoding.Latin1;
    private bool _disposed;
    private long _position = 0; // tracked internally; PipeWriterStream doesn't support .Position

    /// <param name="expectedObjectCount">
    /// Hint for xref list pre-allocation. Each page contributes 2 objects
    /// (page dict + content stream); add a few for catalog, info, and page tree.
    /// Without a hint the list starts at capacity 4 and doubles many times,
    /// creating multiple Large Object Heap arrays for large documents.
    /// </param>
    public PdfWriter(Stream output, int expectedObjectCount = 64)
    {
        _output = output ?? throw new ArgumentNullException(nameof(output));
        _xrefEntries = new List<(int ObjNum, long Offset)>(expectedObjectCount);
    }

    /// <summary>Current byte position in the output stream.</summary>
    public long Position => _position;

    /// <summary>Total number of objects allocated so far.</summary>
    public int ObjectCount => _objectCounter;

    /// <summary>Atomically allocates the next object number.</summary>
    public int AllocateObjectNumber() => Interlocked.Increment(ref _objectCounter);

    // â”€â”€ Raw write helpers â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    public void WriteRaw(string text)
    {
        if (text.Length == 0) return;
        byte[] rented = ArrayPool<byte>.Shared.Rent(text.Length);
        try
        {
            int written = _latin1.GetBytes(text, 0, text.Length, rented, 0);
            _output.Write(rented, 0, written);
            _position += written;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rented);
        }
    }

    public void WriteRaw(ReadOnlySpan<byte> data)
    {
        _output.Write(data);
        _position += data.Length;
    }

    public void WriteLine(string text)
    {
        WriteRaw(text);
        _output.WriteByte(0x0A); // \n
        _position++;
    }

    public void WriteNewLine()
    {
        _output.WriteByte(0x0A);
        _position++;
    }

    public void WriteBytes(byte[] data)
    {
        _output.Write(data, 0, data.Length);
        _position += data.Length;
    }

    public void WriteBytes(ReadOnlySpan<byte> data)
    {
        _output.Write(data);
        _position += data.Length;
    }

    // â”€â”€ PDF structure helpers â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    /// <summary>
    /// Writes the PDF header: version comment and binary-indicator comment.
    /// Must be called first.
    /// </summary>
    public void WriteHeader()
    {
        WriteRaw("%PDF-1.7\n");
        // Four high-bit bytes signal to transfer agents that this is binary
        ReadOnlySpan<byte> binary = [ 0x25, 0xE2, 0xE3, 0xCF, 0xD3, 0x0A ];
        _output.Write(binary);
        _position += binary.Length;
    }

    /// <summary>
    /// Writes an indirect PDF object (N G obj â€¦ endobj), recording its
    /// byte offset for the cross-reference table.
    /// </summary>
    public void WriteObject(PdfObject obj)
    {
        long offset = _position;
        _xrefEntries.Add((obj.ObjectNumber, offset));
        WriteRaw($"{obj.ObjectNumber} {obj.Generation} obj\n");
        obj.WriteTo(this);
        WriteRaw("\nendobj\n\n");
    }

    /// <summary>
    /// Writes the cross-reference table, trailer dictionary, startxref, and %%EOF.
    /// Must be called after all objects have been written.
    /// </summary>
    public void WriteXRefAndTrailer(int catalogObjNum, int? infoObjNum, int totalObjects)
    {
        long xrefOffset = _position;

        var sorted = _xrefEntries
            .OrderBy(e => e.ObjNum)
            .ToList();

        WriteLine("xref");
        // Section: objects 0 through totalObjects
        WriteLine($"0 {totalObjects + 1}");
        // Object 0 is always the free-list head
        WriteLine("0000000000 65535 f ");

        int next = 1;
        foreach (var (objNum, offset) in sorted)
        {
            // Fill any gaps with free entries
            while (next < objNum)
            {
                WriteLine("0000000000 00000 f ");
                next++;
            }
            // Each entry is exactly 20 bytes: oooooooooo ggggg n \r\n  (or space \n)
            WriteRaw($"{offset:D10} {0:D5} n \n");
            next = objNum + 1;
        }

        // Trailer
        WriteLine("trailer");
        WriteRaw($"<< /Size {totalObjects + 1} /Root {catalogObjNum} 0 R");
        if (infoObjNum.HasValue)
            WriteRaw($" /Info {infoObjNum.Value} 0 R");
        WriteRaw(" >>\n");

        WriteLine("startxref");
        WriteLine(xrefOffset.ToString(CultureInfo.InvariantCulture));
        WriteRaw("%%EOF\n");
    }

    public void Flush() => _output.Flush();

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            // Do not close _output â€” caller owns it
        }
    }
}

