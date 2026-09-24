namespace VeloxPdf.Styling;

using VeloxPdf.Color;
using VeloxPdf.Fonts;

public sealed class TableStyle
{
    public bool ShowBorder { get; set; } = true;
    public float BorderWidth { get; set; } = 0.5f;
    public PdfColor BorderColor { get; set; } = PdfColor.FromHex("#D1D5DB");
    public PdfColor HeaderBackgroundColor { get; set; } = PdfColor.FromHex("#2563EB");
    public PdfColor HeaderTextColor { get; set; } = PdfColor.White;

    public TextStyle HeaderTextStyle { get; set; } = new TextStyle
    {
        Font = Type1Font.HelveticaBold,
        FontSize = 10,
        Color = PdfColor.White,
        Bold = true
    };

    public bool StripedRows { get; set; } = true;
    public PdfColor StripedRowColor { get; set; } = PdfColor.FromHex("#F9FAFB");
    public PdfColor DefaultRowColor { get; set; } = PdfColor.White;
    public float CellPaddingLeft { get; set; } = 6f;
    public float CellPaddingTop { get; set; } = 4f;
    public float CellPaddingRight { get; set; } = 6f;
    public float CellPaddingBottom { get; set; } = 4f;
    public float RowMinHeight { get; set; } = 0f;
    public float HeaderHeight { get; set; } = 0f;
    public bool RepeatHeaderOnNewPage { get; set; } = true;

    public TextStyle DataTextStyle { get; set; } = new TextStyle { FontSize = 9 };
}
