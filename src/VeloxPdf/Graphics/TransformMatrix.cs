namespace VeloxPdf.Graphics;

/// <summary>
/// A PDF transformation matrix [a b c d e f] used for coordinate transforms.
/// </summary>
public readonly struct TransformMatrix
{
    private static readonly System.Globalization.CultureInfo Inv =
        System.Globalization.CultureInfo.InvariantCulture;

    public readonly float A, B, C, D, E, F;

    public TransformMatrix(float a, float b, float c, float d, float e, float f)
    {
        A = a; B = b; C = c; D = d; E = e; F = f;
    }

    public static readonly TransformMatrix Identity = new(1, 0, 0, 1, 0, 0);

    public static TransformMatrix Translation(float tx, float ty) =>
        new(1, 0, 0, 1, tx, ty);

    public static TransformMatrix Scale(float sx, float sy) =>
        new(sx, 0, 0, sy, 0, 0);

    public static TransformMatrix Scale(float sx, float sy, float cx, float cy) =>
        new(sx, 0, 0, sy, cx - sx * cx, cy - sy * cy);

    public static TransformMatrix Rotation(float angleRadians)
    {
        float cos = MathF.Cos(angleRadians);
        float sin = MathF.Sin(angleRadians);
        return new(cos, sin, -sin, cos, 0, 0);
    }

    public static TransformMatrix Rotation(float angleRadians, float cx, float cy)
    {
        float cos = MathF.Cos(angleRadians);
        float sin = MathF.Sin(angleRadians);
        return new(
            cos, sin, -sin, cos,
            cx - cos * cx + sin * cy,
            cy - sin * cx - cos * cy);
    }

    /// <summary>Multiplies this matrix by another (this × other).</summary>
    public TransformMatrix Multiply(TransformMatrix o) => new(
        A * o.A + B * o.C,
        A * o.B + B * o.D,
        C * o.A + D * o.C,
        C * o.B + D * o.D,
        E * o.A + F * o.C + o.E,
        E * o.B + F * o.D + o.F);

    /// <summary>Transforms a point using this matrix.</summary>
    public (float x, float y) TransformPoint(float x, float y) =>
        (A * x + C * y + E, B * x + D * y + F);

    private static string Fmt(float v) => v.ToString("0.####", Inv);

    public override string ToString() =>
        $"{Fmt(A)} {Fmt(B)} {Fmt(C)} {Fmt(D)} {Fmt(E)} {Fmt(F)}";
}
