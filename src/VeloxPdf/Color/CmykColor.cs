using VeloxPdf.Graphics;

namespace VeloxPdf.Color;

/// <summary>A CMYK color with float components in [0, 1].</summary>
public sealed class CmykColor : PdfColor
{
    private static readonly System.Globalization.CultureInfo Inv =
        System.Globalization.CultureInfo.InvariantCulture;

    public float C { get; }
    public float M { get; }
    public float Y { get; }
    public float K { get; }

    public CmykColor(float c, float m, float y, float k)
    {
        C = Math.Clamp(c, 0f, 1f);
        M = Math.Clamp(m, 0f, 1f);
        Y = Math.Clamp(y, 0f, 1f);
        K = Math.Clamp(k, 0f, 1f);
    }

    private static string F(float v) => v.ToString("0.####", Inv);

    public override void WriteSetFill(ContentStream cs)
        => cs.AppendRaw($"{F(C)} {F(M)} {F(Y)} {F(K)} k\n");

    public override void WriteSetStroke(ContentStream cs)
        => cs.AppendRaw($"{F(C)} {F(M)} {F(Y)} {F(K)} K\n");

    public override bool Equals(object? obj)
        => obj is CmykColor other && other.C == C && other.M == M && other.Y == Y && other.K == K;

    public override int GetHashCode() => HashCode.Combine(C, M, Y, K);

    public override string ToString() => $"cmyk({C},{M},{Y},{K})";
}
