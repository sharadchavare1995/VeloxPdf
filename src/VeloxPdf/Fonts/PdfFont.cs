namespace VeloxPdf.Fonts;

using VeloxPdf.Primitives;

/// <summary>Abstract base class for all PDF fonts.</summary>
public abstract class PdfFont
{
    /// <summary>Font alias used in PDF resource dictionary, e.g. "F1".</summary>
    public string Alias { get; set; } = "F1";

    /// <summary>PostScript / BaseFont name, e.g. "Helvetica".</summary>
    public abstract string BaseFont { get; }

    /// <summary>Measure total width of a text span at the given font size (in points).</summary>
    public abstract float MeasureWidth(ReadOnlySpan<char> text, float fontSize);

    /// <summary>Measure width of a single character at the given font size (in points).</summary>
    public abstract float MeasureWidth(char c, float fontSize);

    /// <summary>Build the PDF font dictionary object for this font.</summary>
    public abstract PdfDictionary BuildFontDictionary();

    /// <summary>
    /// Return any additional PDF objects required by this font
    /// (e.g. FontDescriptor + embedded stream for TrueType).
    /// These objects must already have ObjectNumbers assigned.
    /// </summary>
    public abstract PdfObject[] GetAdditionalObjects();

    /// <summary>Typical line height = fontSize * 1.2. Override for specific fonts.</summary>
    public virtual float LineHeight(float fontSize) => fontSize * 1.2f;

    /// <summary>Ascender height above baseline in points.</summary>
    public virtual float Ascender(float fontSize) => fontSize * 0.8f;

    /// <summary>Descender depth below baseline in points (positive = below).</summary>
    public virtual float Descender(float fontSize) => fontSize * 0.2f;

    /// <summary>Convenience: measure width of a full string.</summary>
    public float MeasureTextWidth(string text, float fontSize)
    {
        if (string.IsNullOrEmpty(text)) return 0f;
        float total = 0f;
        for (int i = 0; i < text.Length; i++)
            total += MeasureWidth(text[i], fontSize);
        return total;
    }
}
