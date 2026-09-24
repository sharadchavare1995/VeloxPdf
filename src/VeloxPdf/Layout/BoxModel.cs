namespace VeloxPdf.Layout;

public readonly struct BoxModel
{
    public float Left { get; init; }
    public float Top { get; init; }
    public float Right { get; init; }
    public float Bottom { get; init; }

    public BoxModel(float left, float top, float right, float bottom)
    {
        Left = left;
        Top = top;
        Right = right;
        Bottom = bottom;
    }

    public static readonly BoxModel Empty = new(0, 0, 0, 0);

    public static BoxModel Uniform(float all) => new(all, all, all, all);

    public static BoxModel Symmetric(float horizontal, float vertical) =>
        new(horizontal, vertical, horizontal, vertical);

    public float Horizontal => Left + Right;
    public float Vertical => Top + Bottom;
    public float TotalWidth => Left + Right;
    public float TotalHeight => Top + Bottom;
}
