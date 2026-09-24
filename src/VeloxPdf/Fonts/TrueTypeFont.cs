namespace VeloxPdf.Fonts;

using VeloxPdf.Primitives;

/// <summary>
/// A TrueType / OpenType font loaded from raw TTF bytes.
/// Supports full font embedding in the output PDF.
/// </summary>
public sealed class TrueTypeFont : PdfFont
{
    private readonly byte[] _ttfData;
    private readonly TrueTypeParser _parser;
    private readonly float _unitsPerEm;
    private readonly float[] _glyphWidths;  // chars 32–126 (95 entries)
    private readonly string _postScriptName;
    private readonly float _ascender1000;   // in 1/1000 units
    private readonly float _descender1000;
    private readonly float _capHeight1000;
    private readonly float _italicAngle;
    private readonly bool _isFixedPitch;

    private const int FirstChar = 32;
    private const int LastChar  = 126;

    public TrueTypeFont(byte[] ttfData, string alias = "TF1")
    {
        _ttfData = ttfData ?? throw new ArgumentNullException(nameof(ttfData));
        Alias = alias;
        _parser = new TrueTypeParser(ttfData);
        _unitsPerEm     = _parser.UnitsPerEm();
        _glyphWidths    = _parser.GetGlyphWidths(FirstChar, LastChar);
        _postScriptName = _parser.ParsePostScriptName();
        _ascender1000   = _parser.Ascender();
        _descender1000  = _parser.Descender();
        _capHeight1000  = _parser.CapHeight();
        _italicAngle    = _parser.ItalicAngle();
        _isFixedPitch   = _parser.IsFixedPitch();
    }

    public TrueTypeFont(Stream stream, string alias = "TF1")
        : this(ReadFully(stream), alias) { }

    private static byte[] ReadFully(Stream stream)
    {
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return ms.ToArray();
    }

    public override string BaseFont => _postScriptName;

    public override float MeasureWidth(ReadOnlySpan<char> text, float fontSize)
    {
        float total = 0f;
        for (int i = 0; i < text.Length; i++)
            total += MeasureWidth(text[i], fontSize);
        return total;
    }

    public override float MeasureWidth(char c, float fontSize)
    {
        int code = c;
        if (code >= FirstChar && code <= LastChar)
        {
            float w = _glyphWidths[code - FirstChar];
            return w * fontSize / 1000f;
        }
        return 500f * fontSize / 1000f; // fallback for unmapped chars
    }

    public override float Ascender(float fontSize)  => _ascender1000  * fontSize / 1000f;
    public override float Descender(float fontSize) => _descender1000 * fontSize / 1000f;
    public override float LineHeight(float fontSize) => fontSize * 1.2f;

    // ── PDF object builders ─────────────────────────────────────────────────

    public override PdfDictionary BuildFontDictionary()
    {
        var dict = new PdfDictionary();
        dict.Set("Type",     "Font");
        dict.Set("Subtype",  "TrueType");
        dict.Set("BaseFont", _postScriptName);
        dict.Set("Encoding", "WinAnsiEncoding");
        dict.Set("FirstChar", FirstChar);
        dict.Set("LastChar",  LastChar);

        var widthsArr = new PdfArray();
        for (int i = 0; i < _glyphWidths.Length; i++)
            widthsArr.Add((int)Math.Round(_glyphWidths[i]));
        dict.Set("Widths", widthsArr);

        // FontDescriptor reference is set externally by PdfDocument.Save()
        return dict;
    }

    /// <summary>Build the FontDescriptor dictionary (without /FontFile2 — added externally).</summary>
    public PdfDictionary BuildFontDescriptor()
    {
        var desc = new PdfDictionary();
        desc.Set("Type",     "FontDescriptor");
        desc.Set("FontName", _postScriptName);

        // Flags: bit 1 = FixedPitch, bit 6 = Nonsymbolic
        int flags = _isFixedPitch ? 0b0000001 | 0b0100000 : 0b0100000;
        desc.Set("Flags", flags);

        var bbox = new PdfArray();
        bbox.Add(0f);
        bbox.Add(_descender1000);
        bbox.Add(1000f);
        bbox.Add(_ascender1000);
        desc.Set("FontBBox",    bbox);
        desc.Set("ItalicAngle", _italicAngle);
        desc.Set("Ascent",      _ascender1000);
        desc.Set("Descent",     _descender1000);
        desc.Set("CapHeight",   _capHeight1000 > 0 ? _capHeight1000 : _ascender1000 * 0.7f);
        desc.Set("StemV",       80f);
        return desc;
    }

    /// <summary>Raw TTF bytes to embed as /FontFile2 in the FontDescriptor.</summary>
    public byte[] GetFontProgramBytes() => _ttfData;

    /// <summary>Returns empty — extra objects (FontDescriptor + stream) are handled by PdfDocument.</summary>
    public override PdfObject[] GetAdditionalObjects() => Array.Empty<PdfObject>();
}
