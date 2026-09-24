namespace VeloxPdf.Document;

public readonly struct PdfMargins
{
    public float Left { get; init; }
    public float Top { get; init; }
    public float Right { get; init; }
    public float Bottom { get; init; }

    public PdfMargins(float left, float top, float right, float bottom)
    { Left = left; Top = top; Right = right; Bottom = bottom; }

    public static readonly PdfMargins Default = new(36f, 36f, 36f, 36f);  // ~0.5 inch
    public static readonly PdfMargins Narrow = new(18f, 18f, 18f, 18f);   // ~0.25 inch
    public static readonly PdfMargins Wide = new(72f, 72f, 72f, 72f);     // ~1 inch
    public static readonly PdfMargins None = new(0f, 0f, 0f, 0f);

    public static PdfMargins Uniform(float all) => new(all, all, all, all);
    public static PdfMargins Symmetric(float horizontal, float vertical) =>
        new(horizontal, vertical, horizontal, vertical);

    public float Horizontal => Left + Right;
    public float Vertical => Top + Bottom;
    public override string ToString() => $"L={Left} T={Top} R={Right} B={Bottom}";
}
