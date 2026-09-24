namespace VeloxPdf.Styling;

using VeloxPdf.Color;

public enum BorderType { None, Solid, Dashed, Dotted }

public sealed class BorderStyle
{
    public float Width { get; init; } = 0.5f;
    public PdfColor Color { get; init; } = PdfColor.Black;
    public BorderType Type { get; init; } = BorderType.Solid;
    public float[] DashPattern { get; init; } = Array.Empty<float>();

    public static readonly BorderStyle None = new() { Type = BorderType.None, Width = 0 };

    public static BorderStyle Solid(float width, PdfColor color) =>
        new() { Width = width, Color = color, Type = BorderType.Solid };

    public static BorderStyle Dashed(float width, PdfColor color) =>
        new() { Width = width, Color = color, Type = BorderType.Dashed, DashPattern = [3f, 2f] };

    public static BorderStyle Dotted(float width, PdfColor color) =>
        new() { Width = width, Color = color, Type = BorderType.Dotted, DashPattern = [1f, 2f] };
}
