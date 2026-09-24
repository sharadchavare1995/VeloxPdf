namespace VeloxPdf.Document;

public readonly struct PdfPageSize
{
    public float Width { get; init; }
    public float Height { get; init; }

    public PdfPageSize(float width, float height)
    { Width = width; Height = height; }

    public static readonly PdfPageSize A4 = new(595f, 842f);
    public static readonly PdfPageSize A3 = new(842f, 1190f);
    public static readonly PdfPageSize A5 = new(420f, 595f);
    public static readonly PdfPageSize Letter = new(612f, 792f);
    public static readonly PdfPageSize Legal = new(612f, 1008f);
    public static readonly PdfPageSize A4Landscape = new(842f, 595f);
    public static readonly PdfPageSize LetterLandscape = new(792f, 612f);

    public static PdfPageSize Custom(float width, float height) => new(width, height);

    public PdfPageSize ToLandscape() => new(Height, Width);
    public PdfPageSize ToPortrait() => new(
        Math.Min(Width, Height), Math.Max(Width, Height));

    public override string ToString() => $"{Width}x{Height}pt";
}
