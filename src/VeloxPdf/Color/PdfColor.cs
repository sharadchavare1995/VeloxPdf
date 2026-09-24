using VeloxPdf.Graphics;

namespace VeloxPdf.Color;

/// <summary>Abstract base class for all PDF colors.</summary>
public abstract class PdfColor
{
    /// <summary>Writes the fill color operator to the content stream.</summary>
    public abstract void WriteSetFill(ContentStream cs);

    /// <summary>Writes the stroke color operator to the content stream.</summary>
    public abstract void WriteSetStroke(ContentStream cs);

    // ── Factories ────────────────────────────────────────────────────────────

    public static PdfColor FromHex(string hex)
    {
        if (string.IsNullOrWhiteSpace(hex))
            throw new ArgumentNullException(nameof(hex));

        hex = hex.TrimStart('#');

        if (hex.Length == 3)
            hex = new string(new[] { hex[0], hex[0], hex[1], hex[1], hex[2], hex[2] });

        if (hex.Length != 6)
            throw new FormatException($"Invalid hex color: #{hex}");

        byte r = Convert.ToByte(hex[0..2], 16);
        byte g = Convert.ToByte(hex[2..4], 16);
        byte b = Convert.ToByte(hex[4..6], 16);
        return new RgbColor(r, g, b);
    }

    public static PdfColor FromRgb(byte r, byte g, byte b) => new RgbColor(r, g, b);
    public static PdfColor FromRgbF(float r, float g, float b) => new RgbColor(r, g, b);
    public static PdfColor FromCmyk(float c, float m, float y, float k) => new CmykColor(c, m, y, k);

    // ── Predefined colors ─────────────────────────────────────────────────────

    public static readonly PdfColor Black       = new RgbColor(0f,    0f,    0f);
    public static readonly PdfColor White       = new RgbColor(1f,    1f,    1f);
    public static readonly PdfColor Red         = new RgbColor(1f,    0f,    0f);
    public static readonly PdfColor Green       = new RgbColor(0f,    0.502f,0f);
    public static readonly PdfColor Blue        = new RgbColor(0f,    0f,    1f);
    public static readonly PdfColor Gray        = new RgbColor(0.502f,0.502f,0.502f);
    public static readonly PdfColor LightGray   = new RgbColor(0.827f,0.827f,0.827f);
    public static readonly PdfColor DarkGray    = new RgbColor(0.251f,0.251f,0.251f);
    public static readonly PdfColor Orange      = new RgbColor(1f,    0.647f,0f);
    public static readonly PdfColor Yellow      = new RgbColor(1f,    1f,    0f);
    public static readonly PdfColor Purple      = new RgbColor(0.502f,0f,    0.502f);
    public static readonly PdfColor Cyan        = new RgbColor(0f,    1f,    1f);
    public static readonly PdfColor Magenta     = new RgbColor(1f,    0f,    1f);
    public static readonly PdfColor Brown       = new RgbColor(0.647f,0.165f,0.165f);
    public static readonly PdfColor Navy        = new RgbColor(0f,    0f,    0.502f);
    public static readonly PdfColor Teal        = new RgbColor(0f,    0.502f,0.502f);
    public static readonly PdfColor Silver      = new RgbColor(0.753f,0.753f,0.753f);
    public static readonly PdfColor Gold        = new RgbColor(1f,    0.843f,0f);
    public static readonly PdfColor Transparent = new RgbColor(0f,    0f,    0f) { IsTransparent = true };
    public static readonly PdfColor PrimaryBlue = FromHex("#2563EB");
    public static readonly PdfColor DarkBlue    = FromHex("#1E3A8A");

    // Used internally to signal "no color" (transparent)
    public bool IsTransparent { get; private set; }
}
