namespace VeloxPdf.Primitives;

/// <summary>
/// PDF string object. Stored as Latin-1 (ISO-8859-1) bytes internally.
/// Can be written as a literal (escaped parentheses) or hexadecimal string.
/// </summary>
public sealed class PdfString : PdfObject
{
    private static readonly Encoding _latin1 = Encoding.Latin1;

    private readonly byte[] _bytes;

    /// <summary>Output as hex string &lt;hexbytes&gt; instead of literal (string).</summary>
    public bool UseHex { get; set; }

    public PdfString(string text)
    {
        _bytes = _latin1.GetBytes(text);
    }

    public PdfString(byte[] bytes)
    {
        _bytes = (byte[])bytes.Clone();
    }

    /// <summary>Decoded string value.</summary>
    public string Value => _latin1.GetString(_bytes);

    public byte[] Bytes => _bytes;

    public override void WriteTo(IO.PdfWriter writer)
    {
        if (UseHex)
        {
            writer.WriteRaw("<");
            Span<char> hexBuf = stackalloc char[2];
            foreach (byte b in _bytes)
            {
                b.TryFormat(hexBuf, out _, "X2");
                writer.WriteRaw(new string(hexBuf));
            }
            writer.WriteRaw(">");
        }
        else
        {
            writer.WriteRaw("(");
            foreach (byte b in _bytes)
            {
                switch (b)
                {
                    case (byte)'(':  writer.WriteRaw("\\("); break;
                    case (byte)')':  writer.WriteRaw("\\)"); break;
                    case (byte)'\\': writer.WriteRaw("\\\\"); break;
                    case 0x0D:       writer.WriteRaw("\\r"); break;
                    case 0x0A:       writer.WriteRaw("\\n"); break;
                    default:
                        writer.WriteRaw(new ReadOnlySpan<byte>(new[] { b }));
                        break;
                }
            }
            writer.WriteRaw(")");
        }
    }

    public override string ToString() => Value;
}

