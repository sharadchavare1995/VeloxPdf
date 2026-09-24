namespace VeloxPdf.Primitives;

/// <summary>
/// PDF stream object. Contains a dictionary and a byte sequence.
/// Handles compression via the ICompressor abstraction.
/// </summary>
public sealed class PdfStream : PdfObject
{
    private static readonly Encoding _latin1 = Encoding.Latin1;

    public PdfDictionary Dictionary { get; } = new();

    private byte[] _encodedData = Array.Empty<byte>();

    public int Length => _encodedData.Length;

    /// <summary>
    /// Sets the stream content from raw bytes, applying the given compressor.
    /// Automatically sets /Length and /Filter in the stream dictionary.
    /// </summary>
    public void SetRawData(ReadOnlySpan<byte> data, Compression.ICompressor compressor)
    {
        _encodedData = compressor.Compress(data);
        Dictionary.Set("Length", _encodedData.Length);

        if (compressor.FilterName is not null)
            Dictionary.Set("Filter", new PdfName(compressor.FilterName));
        else
            Dictionary.Remove("Filter");
    }

    /// <summary>
    /// Encodes the given text as Latin-1 bytes, then compresses and stores it.
    /// </summary>
    public void SetTextData(string text, Compression.ICompressor compressor)
    {
        byte[] bytes = _latin1.GetBytes(text);
        SetRawData(bytes, compressor);
    }

    public override void WriteTo(IO.PdfWriter writer)
    {
        Dictionary.WriteTo(writer);
        writer.WriteRaw("\nstream\n");
        writer.WriteBytes(_encodedData);
        writer.WriteRaw("\nendstream");
    }
}

