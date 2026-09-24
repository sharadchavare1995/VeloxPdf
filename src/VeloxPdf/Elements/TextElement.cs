namespace VeloxPdf.Elements;

using VeloxPdf.Color;
using VeloxPdf.Document;
using VeloxPdf.Fonts;
using VeloxPdf.Layout;
using VeloxPdf.Styling;

public sealed class TextElement : IPdfElement
{
    public string Text { get; set; } = "";
    public TextStyle Style { get; set; } = TextStyle.Default;
    public bool CanSplit => true;

    private PdfFont ResolveFont(RenderContext ctx)
    {
        var font = Style.Font;
        if (font == null) font = ctx.DefaultFont;
        if (Style.Bold && (font == Type1Font.Helvetica || font == null))
            font = Type1Font.HelveticaBold;
        return font ?? Type1Font.Helvetica;
    }

    private float ResolveFontSize(RenderContext ctx)
        => Style.FontSize ?? ctx.DefaultFontSize;

    public float MeasureHeight(RenderContext ctx)
    {
        var font = ResolveFont(ctx);
        float fontSize = ResolveFontSize(ctx);
        float lineH = fontSize * Style.LineHeightMultiplier;
        var lines = TextWrapper.WrapText(Text, ctx.Layout.ContentWidth, font, fontSize);
        return Style.SpaceBefore + lines.Count * lineH + Style.SpaceAfter;
    }

    public void Render(RenderContext ctx)
    {
        if (string.IsNullOrEmpty(Text)) return;

        var font = ResolveFont(ctx);
        float fontSize = ResolveFontSize(ctx);
        float lineH = fontSize * Style.LineHeightMultiplier;
        float contentWidth = ctx.Layout.ContentWidth;

        var lines = TextWrapper.WrapText(Text, contentWidth, font, fontSize);
        var color = Style.Color ?? PdfColor.Black;

        ctx.Layout.Advance(Style.SpaceBefore);

        // Register font on current page
        string alias = ctx.CurrentPage.RegisterFont(font);

        int lineIdx = 0;
        while (lineIdx < lines.Count)
        {
            if (!ctx.Layout.CanFit(lineH))
            {
                ctx.RequestNewPage();
                alias = ctx.CurrentPage.RegisterFont(font);
            }

            var line = lines[lineIdx];
            float y = ctx.Layout.ToPdfY(ctx.Layout.CursorY + fontSize);
            float x = ctx.Layout.ContentLeft + (lineIdx == 0 ? Style.FirstLineIndent : 0f);

            // Compute X based on alignment
            switch (Style.Alignment)
            {
                case PdfAlignment.Center:
                    x = ctx.Layout.ContentLeft + (contentWidth - line.Width) / 2f;
                    break;
                case PdfAlignment.Right:
                    x = ctx.Layout.ContentRight - line.Width;
                    break;
                case PdfAlignment.Left:
                default:
                    // x stays at ContentLeft (+ indent for first line)
                    break;
            }

            var cs = ctx.CurrentPage.ContentStream;
            cs.BeginText();
            cs.SetFont(alias, fontSize);
            cs.SetFillColor(color);
            cs.SetAbsolutePos(x, y);

            // Justification: adjust word spacing for non-last lines
            if (Style.Alignment == PdfAlignment.Justify && !line.IsLastInParagraph)
            {
                int spaceCount = CountSpaces(line.Content);
                if (spaceCount > 0)
                {
                    float extraSpace = (contentWidth - line.Width) / spaceCount;
                    cs.SetWordSpacing(extraSpace);
                }
            }

            cs.ShowText(line.Content);

            if (Style.Alignment == PdfAlignment.Justify)
                cs.SetWordSpacing(0f);

            cs.EndText();

            // Underline decoration
            if (Style.Underline)
            {
                float underlineY = y - fontSize * 0.12f;
                cs.SaveState();
                cs.SetStrokeColor(color);
                cs.SetLineWidth(fontSize * 0.05f);
                cs.MoveTo(x, underlineY);
                cs.LineTo(x + line.Width, underlineY);
                cs.Stroke();
                cs.RestoreState();
            }

            // Strikethrough decoration
            if (Style.Strikethrough)
            {
                float strikeY = y + fontSize * 0.3f;
                cs.SaveState();
                cs.SetStrokeColor(color);
                cs.SetLineWidth(fontSize * 0.05f);
                cs.MoveTo(x, strikeY);
                cs.LineTo(x + line.Width, strikeY);
                cs.Stroke();
                cs.RestoreState();
            }

            ctx.Layout.Advance(lineH);
            lineIdx++;
        }

        ctx.Layout.Advance(Style.SpaceAfter);
    }

    private static int CountSpaces(string text)
    {
        int count = 0;
        for (int i = 0; i < text.Length; i++)
            if (text[i] == ' ') count++;
        return count;
    }
}
