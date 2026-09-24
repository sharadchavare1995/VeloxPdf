namespace VeloxPdf.IO;

/// <summary>
/// Tracks byte offsets for all indirect PDF objects so that the cross-reference
/// table can be written at the end of the PDF stream.
/// </summary>
public sealed class XrefTable
{
    /// <summary>Single cross-reference entry.</summary>
    public readonly record struct XrefEntry(int ObjNum, long Offset, bool InUse = true);

    private readonly List<XrefEntry> _entries;

    public XrefTable(int capacityHint = 1024)
    {
        _entries = new List<XrefEntry>(capacityHint);
    }

    /// <summary>Records an object's byte offset.</summary>
    public void Add(int objNum, long offset)
        => _entries.Add(new XrefEntry(objNum, offset));

    public int Count => _entries.Count;

    public IReadOnlyList<XrefEntry> Entries => _entries;

    /// <summary>
    /// Writes the traditional PDF cross-reference table section to the given stream.
    /// The stream is left open.
    /// </summary>
    public void WriteXrefTable(Stream output)
    {
        var sorted = _entries.OrderBy(e => e.ObjNum).ToList();
        int maxObj = sorted.Count > 0 ? sorted[^1].ObjNum : 0;

        // Use a StreamWriter but flush manually; keep stream open
        var sw = new StreamWriter(output, Encoding.ASCII, bufferSize: 4096, leaveOpen: true)
        {
            NewLine = "\n"
        };

        sw.WriteLine("xref");
        sw.WriteLine($"0 {maxObj + 1}");
        // Free-list head — object 0 is always free
        sw.WriteLine("0000000000 65535 f ");

        int nextExpected = 1;
        foreach (var entry in sorted)
        {
            while (nextExpected < entry.ObjNum)
            {
                sw.WriteLine("0000000000 00000 f ");
                nextExpected++;
            }
            // 20-byte format: oooooooooo ggggg n \n  (note trailing space before \n per spec)
            sw.WriteLine($"{entry.Offset:D10} {0:D5} n ");
            nextExpected = entry.ObjNum + 1;
        }

        sw.Flush();
    }

    /// <summary>
    /// Pre-allocates the entry list for estimated page count to avoid resizes.
    /// </summary>
    public void PreAllocate(long estimatedObjects)
    {
        if (estimatedObjects > _entries.Capacity)
            _entries.Capacity = (int)Math.Min(estimatedObjects, int.MaxValue);
    }
}
