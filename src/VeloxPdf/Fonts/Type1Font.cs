namespace VeloxPdf.Fonts;

using VeloxPdf.Fonts.AfmData;
using VeloxPdf.Primitives;

/// <summary>
/// A PDF Type1 (built-in) font. Uses AFM width tables for measurement.
/// No font embedding required for the 14 standard PDF fonts.
/// </summary>
public sealed class Type1Font : PdfFont
{
    private readonly float[] _charWidths; // 256-element array, widths in 1/1000 text unit
    private readonly string _baseFont;

    public Type1Font(string alias, string baseFontName, float[] charWidths)
    {
        if (charWidths.Length < 128)
            throw new ArgumentException("charWidths must have at least 128 entries.", nameof(charWidths));
        Alias = alias;
        _baseFont = baseFontName;
        _charWidths = charWidths;
    }

    public override string BaseFont => _baseFont;

    public override float MeasureWidth(ReadOnlySpan<char> text, float fontSize)
    {
        float total = 0f;
        for (int i = 0; i < text.Length; i++)
        {
            int code = text[i];
            total += code < _charWidths.Length ? _charWidths[code] : 500f;
        }
        return total * fontSize / 1000f;
    }

    public override float MeasureWidth(char c, float fontSize)
    {
        int code = c;
        float w = code < _charWidths.Length ? _charWidths[code] : 500f;
        return w * fontSize / 1000f;
    }

    public override PdfDictionary BuildFontDictionary()
    {
        var dict = new PdfDictionary();
        dict.Set("Type", "Font");
        dict.Set("Subtype", "Type1");
        dict.Set("BaseFont", _baseFont);
        dict.Set("Encoding", "WinAnsiEncoding");
        return dict;
    }

    public override PdfObject[] GetAdditionalObjects() => Array.Empty<PdfObject>();

    // ── Standard 14 PDF Built-in Fonts ─────────────────────────────────────

    /// <summary>Helvetica (sans-serif regular). Alias F1.</summary>
    public static readonly Type1Font Helvetica =
        new("F1", "Helvetica", HelveticaAfm.Widths);

    /// <summary>Helvetica-Bold. Alias F2.</summary>
    public static readonly Type1Font HelveticaBold =
        new("F2", "Helvetica-Bold", HelveticaBoldAfm.Widths);

    /// <summary>Helvetica-Oblique (italic). Alias F3.</summary>
    public static readonly Type1Font HelveticaOblique =
        new("F3", "Helvetica-Oblique", HelveticaAfm.Widths);

    /// <summary>Times-Roman (serif regular). Alias F4.</summary>
    public static readonly Type1Font TimesRoman =
        new("F4", "Times-Roman", TimesRomanAfm.Widths);

    /// <summary>Times-Bold. Alias F5.</summary>
    public static readonly Type1Font TimesBold =
        new("F5", "Times-Bold", TimesRomanAfm.Widths);

    /// <summary>Courier (monospaced regular). Alias F6.</summary>
    public static readonly Type1Font Courier =
        new("F6", "Courier", CourierAfm.Widths);

    /// <summary>Courier-Bold. Alias F7.</summary>
    public static readonly Type1Font CourierBold =
        new("F7", "Courier-Bold", CourierAfm.Widths);

    /// <summary>Symbol font. Alias F8.</summary>
    private static readonly float[] _symbolWidths = BuildSymbolWidths();
    public static readonly Type1Font Symbol =
        new("F8", "Symbol", _symbolWidths);

    private static float[] BuildSymbolWidths()
    {
        var w = new float[256];
        for (int i = 32; i < 256; i++) w[i] = 600f;
        return w;
    }
}
