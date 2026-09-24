namespace VeloxPdf.Styling;

using VeloxPdf.Color;
using VeloxPdf.Fonts;

public sealed class TextStyle
{
    public PdfFont? Font { get; init; }
    public float? FontSize { get; init; }
    public PdfColor? Color { get; init; }
    public bool Bold { get; init; }
    public bool Italic { get; init; }
    public bool Underline { get; init; }
    public bool Strikethrough { get; init; }
    public PdfAlignment Alignment { get; init; } = PdfAlignment.Left;
    public float LineHeightMultiplier { get; init; } = 1.4f;
    public float SpaceBefore { get; init; } = 0f;
    public float SpaceAfter { get; init; } = 4f;
    public float FirstLineIndent { get; init; } = 0f;

    public static readonly TextStyle Default = new();

    public static readonly TextStyle Heading1 = new()
    {
        Font = Type1Font.HelveticaBold,
        FontSize = 24,
        Bold = true,
        SpaceBefore = 12,
        SpaceAfter = 8
    };

    public static readonly TextStyle Heading2 = new()
    {
        Font = Type1Font.HelveticaBold,
        FontSize = 18,
        Bold = true,
        SpaceBefore = 10,
        SpaceAfter = 6
    };

    public static readonly TextStyle Heading3 = new()
    {
        Font = Type1Font.HelveticaBold,
        FontSize = 14,
        Bold = true,
        SpaceBefore = 8,
        SpaceAfter = 4
    };

    public static readonly TextStyle Caption = new()
    {
        FontSize = 9,
        Color = PdfColor.Gray,
        SpaceAfter = 2
    };

    public static readonly TextStyle Code = new()
    {
        Font = Type1Font.Courier,
        FontSize = 9,
        SpaceAfter = 4
    };

    public static readonly TextStyle Footnote = new()
    {
        FontSize = 8,
        Color = PdfColor.DarkGray
    };

    public TextStyle WithFont(PdfFont font) => new TextStyle
    {
        Font = font,
        FontSize = FontSize,
        Color = Color,
        Bold = Bold,
        Italic = Italic,
        Underline = Underline,
        Strikethrough = Strikethrough,
        Alignment = Alignment,
        LineHeightMultiplier = LineHeightMultiplier,
        SpaceBefore = SpaceBefore,
        SpaceAfter = SpaceAfter,
        FirstLineIndent = FirstLineIndent
    };

    public TextStyle WithSize(float size) => new TextStyle
    {
        Font = Font,
        FontSize = size,
        Color = Color,
        Bold = Bold,
        Italic = Italic,
        Underline = Underline,
        Strikethrough = Strikethrough,
        Alignment = Alignment,
        LineHeightMultiplier = LineHeightMultiplier,
        SpaceBefore = SpaceBefore,
        SpaceAfter = SpaceAfter,
        FirstLineIndent = FirstLineIndent
    };

    public TextStyle WithColor(PdfColor color) => new TextStyle
    {
        Font = Font,
        FontSize = FontSize,
        Color = color,
        Bold = Bold,
        Italic = Italic,
        Underline = Underline,
        Strikethrough = Strikethrough,
        Alignment = Alignment,
        LineHeightMultiplier = LineHeightMultiplier,
        SpaceBefore = SpaceBefore,
        SpaceAfter = SpaceAfter,
        FirstLineIndent = FirstLineIndent
    };

    public TextStyle WithAlignment(PdfAlignment align) => new TextStyle
    {
        Font = Font,
        FontSize = FontSize,
        Color = Color,
        Bold = Bold,
        Italic = Italic,
        Underline = Underline,
        Strikethrough = Strikethrough,
        Alignment = align,
        LineHeightMultiplier = LineHeightMultiplier,
        SpaceBefore = SpaceBefore,
        SpaceAfter = SpaceAfter,
        FirstLineIndent = FirstLineIndent
    };

    public TextStyle WithBold(bool bold = true) => new TextStyle
    {
        Font = Font,
        FontSize = FontSize,
        Color = Color,
        Bold = bold,
        Italic = Italic,
        Underline = Underline,
        Strikethrough = Strikethrough,
        Alignment = Alignment,
        LineHeightMultiplier = LineHeightMultiplier,
        SpaceBefore = SpaceBefore,
        SpaceAfter = SpaceAfter,
        FirstLineIndent = FirstLineIndent
    };

    public TextStyle WithSpacing(float before, float after) => new TextStyle
    {
        Font = Font,
        FontSize = FontSize,
        Color = Color,
        Bold = Bold,
        Italic = Italic,
        Underline = Underline,
        Strikethrough = Strikethrough,
        Alignment = Alignment,
        LineHeightMultiplier = LineHeightMultiplier,
        SpaceBefore = before,
        SpaceAfter = after,
        FirstLineIndent = FirstLineIndent
    };
}
