using VeloxPdf.Graphics;

namespace VeloxPdf.Color;

/// <summary>An RGB color with float components in [0, 1].</summary>
public sealed class RgbColor : PdfColor
{
    private static readonly System.Globalization.CultureInfo Inv =
        System.Globalization.CultureInfo.InvariantCulture;

    public float R { get; }
    public float G { get; }
    public float B { get; }

    /// <summary>Constructs an RGB color from float components (clamped to [0, 1]).</summary>
    public RgbColor(float r, float g, float b)
    {
        R = Math.Clamp(r, 0f, 1f);
        G = Math.Clamp(g, 0f, 1f);
        B = Math.Clamp(b, 0f, 1f);
    }

    /// <summary>Constructs an RGB color from byte components (0–255).</summary>
    public RgbColor(byte r, byte g, byte b)
        : this(r / 255f, g / 255f, b / 255f) { }

    private static string F(float v) => v.ToString("0.####", Inv);

    public override void WriteSetFill(ContentStream cs)
        => cs.AppendRaw($"{F(R)} {F(G)} {F(B)} rg\n");

    public override void WriteSetStroke(ContentStream cs)
        => cs.AppendRaw($"{F(R)} {F(G)} {F(B)} RG\n");

    public override bool Equals(object? obj)
        => obj is RgbColor other && other.R == R && other.G == G && other.B == B;

    public override int GetHashCode() => HashCode.Combine(R, G, B);

    public override string ToString() =>
        $"rgb({(int)(R * 255)},{(int)(G * 255)},{(int)(B * 255)})";
}
