namespace VeloxPdf.IO;

using System.Buffers;
using System.Buffers.Text;

/// <summary>
/// Zero-allocation helpers for writing PDF tokens into IBufferWriter&lt;byte&gt; sinks.
/// All PDF operators are pre-encoded as static byte arrays to avoid per-call allocations.
/// </summary>
public static class FastPdfFormatter
{
    private static readonly Encoding _latin1 = Encoding.Latin1;

    // Pre-encoded PDF operator bytes (operator + trailing space)
    private static readonly Dictionary<string, byte[]> _opCache =
        new(StringComparer.Ordinal);

    static FastPdfFormatter()
    {
        string[] ops =
        [
            "BT", "ET", "Tf", "Td", "Tm", "Tj", "T*", "TL", "Tc", "Tw", "Tz", "Tr",
            "q", "Q", "S", "s", "f", "f*", "B", "B*", "b", "n", "h", "m", "l", "c",
            "re", "w", "J", "j", "M", "d", "rg", "RG", "g", "G", "k", "K", "cm", "Do",
            "'", "\"", "W", "W*"
        ];
        foreach (string op in ops)
            _opCache[op] = _latin1.GetBytes(op + "\n");
    }

    // â”€â”€ Float â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    /// <summary>Writes a float with max 4 decimal places, no trailing zeros.</summary>
    public static void WriteFloat(IBufferWriter<byte> writer, float value)
    {
        string s = value.ToString("0.####", CultureInfo.InvariantCulture);
        Span<byte> buf = stackalloc byte[32];
        int len = _latin1.GetBytes(s, buf);
        writer.Write(buf[..len]);
    }

    public static string FormatFloat(float value)
        => value.ToString("0.####", CultureInfo.InvariantCulture);

    // â”€â”€ Integer â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    public static void WriteInt(IBufferWriter<byte> writer, int value)
    {
        Span<byte> buf = stackalloc byte[16];
        if (Utf8Formatter.TryFormat(value, buf, out int written))
            writer.Write(buf[..written]);
    }

    // â”€â”€ Operator â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    /// <summary>Writes a pre-cached PDF operator (op + newline).</summary>
    public static void WriteOperator(IBufferWriter<byte> writer, string op)
    {
        if (_opCache.TryGetValue(op, out byte[]? bytes))
            writer.Write(bytes);
        else
        {
            // Fallback for unknown operators
            writer.Write(_latin1.GetBytes(op + "\n"));
        }
    }

    // â”€â”€ Space / newline â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    public static void WriteSpace(IBufferWriter<byte> writer)
    {
        Span<byte> s = stackalloc byte[1] { 0x20 };
        writer.Write(s);
    }

    public static void WriteNewLine(IBufferWriter<byte> writer)
    {
        Span<byte> s = stackalloc byte[1] { 0x0A };
        writer.Write(s);
    }

    // â”€â”€ ASCII text â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    public static void WriteAscii(IBufferWriter<byte> writer, string text)
    {
        // Safe for short strings; for long strings caller should use Latin1 directly
        Span<byte> buf = text.Length <= 256
            ? stackalloc byte[text.Length]
            : new byte[text.Length];
        int len = _latin1.GetBytes(text, buf);
        writer.Write(buf[..len]);
    }
}

