namespace VeloxPdf.Elements;

using VeloxPdf.Color;
using VeloxPdf.Document;
using VeloxPdf.Fonts;
using VeloxPdf.Layout;
using VeloxPdf.Styling;

public sealed class TableElement : IPdfElement
{
    public IReadOnlyList<string> Headers { get; set; } = Array.Empty<string>();
    public List<string[]> Rows { get; set; } = new();
    public TableStyle Style { get; set; } = new();
    public float[]? ColumnWidths { get; set; }
    public PdfAlignment[]? ColumnAlignments { get; set; }
    public bool CanSplit => true;

    private float[] ComputeColumnWidths(float totalWidth)
    {
        if (ColumnWidths != null && ColumnWidths.Length > 0)
        {
            // Determine if proportional (all <= 1.0) or absolute
            float sum = 0f;
            for (int i = 0; i < ColumnWidths.Length; i++) sum += ColumnWidths[i];
            if (sum <= 1.01f)
            {
                float[] abs = new float[ColumnWidths.Length];
                for (int i = 0; i < abs.Length; i++) abs[i] = ColumnWidths[i] * totalWidth;
                return abs;
            }
            return ColumnWidths;
        }

        // Equal split across header count
        int cols = Math.Max(1, Headers.Count);
        float[] widths = new float[cols];
        float each = totalWidth / cols;
        for (int i = 0; i < cols; i++) widths[i] = each;
        return widths;
    }

    private float ComputeRowHeight(string[] cells, float[] colWidths, PdfFont font, float fontSize)
    {
        float maxH = Style.RowMinHeight;
        float lineH = fontSize * 1.2f;
        float padV = Style.CellPaddingTop + Style.CellPaddingBottom;

        for (int i = 0; i < cells.Length && i < colWidths.Length; i++)
        {
            float cellW = colWidths[i] - Style.CellPaddingLeft - Style.CellPaddingRight;
            if (cellW < 1f) cellW = 1f;
            var lines = TextWrapper.WrapText(cells[i] ?? "", cellW, font, fontSize);
            float h = lines.Count * lineH + padV;
            if (h > maxH) maxH = h;
        }
        return Math.Max(maxH, lineH + padV);
    }

    private float ComputeHeaderHeight(float[] colWidths, PdfFont font, float fontSize)
    {
        // Always compute the dynamic height so wrapped headers never push into body rows.
        float dynamicH = ComputeRowHeight(Headers.ToArray(), colWidths, font, fontSize);

        // When a fixed HeaderHeight is configured it acts as a *minimum*, not a cap:
        // if wrapped text needs more space the dynamic height wins.
        if (Style.HeaderHeight > 0)
            return Math.Max(Style.HeaderHeight, dynamicH);

        return dynamicH;
    }

    public float MeasureHeight(RenderContext ctx)
    {
        var dataFont = Style.DataTextStyle.Font ?? ctx.DefaultFont;
        float fontSize = Style.DataTextStyle.FontSize ?? ctx.DefaultFontSize;

        // Use the header font/size when measuring the header row so that the
        // total height is consistent with what Render() will actually draw.
        var headerFont = Style.HeaderTextStyle.Font ?? Type1Font.HelveticaBold;
        float headerFontSize = Style.HeaderTextStyle.FontSize ?? fontSize;

        float[] colWidths = ComputeColumnWidths(ctx.Layout.ContentWidth);
        float headerH = ComputeHeaderHeight(colWidths, headerFont, headerFontSize);
        float total = headerH;
        for (int r = 0; r < Rows.Count; r++)
            total += ComputeRowHeight(Rows[r], colWidths, dataFont, fontSize);
        return total;
    }

    private static void DrawCell(
        Graphics.ContentStream cs,
        string text, float x, float y, float w, float h,
        PdfFont font, float fontSize, string fontAlias,
        PdfAlignment alignment, PdfColor textColor, TableStyle style)
    {
        float cellContentW = w - style.CellPaddingLeft - style.CellPaddingRight;
        if (cellContentW < 1f) cellContentW = 1f;

        var lines = TextWrapper.WrapText(text ?? "", cellContentW, font, fontSize);
        float lineH = fontSize * 1.2f;

        // Text baseline: start from top of cell minus padding and font size
        float textYStart = y + h - style.CellPaddingTop - fontSize;

        cs.BeginText();
        cs.SetFont(fontAlias, fontSize);
        cs.SetFillColor(textColor);

        for (int li = 0; li < lines.Count; li++)
        {
            float lineY = textYStart - li * lineH;
            float lineX = x + style.CellPaddingLeft;

            if (alignment == PdfAlignment.Center)
                lineX = x + (w - lines[li].Width) / 2f;
            else if (alignment == PdfAlignment.Right)
                lineX = x + w - style.CellPaddingRight - lines[li].Width;

            cs.SetAbsolutePos(lineX, lineY);
            cs.ShowText(lines[li].Content);
        }
        cs.EndText();
    }

    private static void DrawBackground(
        Graphics.ContentStream cs, float x, float y, float w, float h, PdfColor color)
    {
        cs.SaveState();
        cs.SetFillColor(color);
        cs.Rectangle(x, y, w, h);
        cs.Fill();
        cs.RestoreState();
    }

    private static void DrawBorder(
        Graphics.ContentStream cs, float x, float y, float w, float h,
        float borderW, PdfColor borderColor)
    {
        cs.SaveState();
        cs.SetStrokeColor(borderColor);
        cs.SetLineWidth(borderW);
        cs.Rectangle(x, y, w, h);
        cs.Stroke();
        cs.RestoreState();
    }

    private void RenderHeaderRow(
        RenderContext ctx, float[] colWidths, float headerH,
        PdfFont headerFont, float headerFontSize, string headerAlias)
    {
        var cs = ctx.CurrentPage.ContentStream;
        var layout = ctx.Layout;
        float tableX = layout.ContentLeft;
        float y = layout.ToPdfY(layout.CursorY + headerH);
        float totalW = 0f;
        for (int i = 0; i < colWidths.Length; i++) totalW += colWidths[i];

        // Header background (full row)
        DrawBackground(cs, tableX, y, totalW, headerH, Style.HeaderBackgroundColor);

        // Draw each header cell
        float cellX = tableX;
        for (int c = 0; c < Headers.Count && c < colWidths.Length; c++)
        {
            var align = ColumnAlignments != null && c < ColumnAlignments.Length
                ? ColumnAlignments[c] : PdfAlignment.Left;

            DrawCell(cs, Headers[c], cellX, y, colWidths[c], headerH,
                headerFont, headerFontSize, headerAlias,
                align, Style.HeaderTextColor, Style);

            if (Style.ShowBorder)
                DrawBorder(cs, cellX, y, colWidths[c], headerH,
                    Style.BorderWidth, Style.BorderColor);

            cellX += colWidths[c];
        }

        layout.Advance(headerH);
    }

    public void Render(RenderContext ctx)
    {
        var dataFont = Style.DataTextStyle.Font ?? ctx.DefaultFont;
        float fontSize = Style.DataTextStyle.FontSize ?? ctx.DefaultFontSize;
        var headerFont = Style.HeaderTextStyle.Font ?? Type1Font.HelveticaBold;
        float headerFontSize = Style.HeaderTextStyle.FontSize ?? fontSize;

        float[] colWidths = ComputeColumnWidths(ctx.Layout.ContentWidth);
        float headerH = ComputeHeaderHeight(colWidths, headerFont, headerFontSize);

        string dataAlias = ctx.CurrentPage.RegisterFont(dataFont);
        string headerAlias = ctx.CurrentPage.RegisterFont(headerFont);

        // Ensure space for at least header row
        if (!ctx.Layout.CanFit(headerH))
            ctx.RequestNewPage();

        RenderHeaderRow(ctx, colWidths, headerH, headerFont, headerFontSize, headerAlias);

        float totalW = 0f;
        for (int i = 0; i < colWidths.Length; i++) totalW += colWidths[i];

        for (int r = 0; r < Rows.Count; r++)
        {
            string[] row = Rows[r];
            float rowH = ComputeRowHeight(row, colWidths, dataFont, fontSize);

            if (!ctx.Layout.CanFit(rowH))
            {
                ctx.RequestNewPage();
                dataAlias = ctx.CurrentPage.RegisterFont(dataFont);
                headerAlias = ctx.CurrentPage.RegisterFont(headerFont);

                if (Style.RepeatHeaderOnNewPage)
                    RenderHeaderRow(ctx, colWidths, headerH, headerFont, headerFontSize, headerAlias);
            }

            var cs = ctx.CurrentPage.ContentStream;
            float tableX = ctx.Layout.ContentLeft;
            float y = ctx.Layout.ToPdfY(ctx.Layout.CursorY + rowH);

            // Row background (striped)
            bool isOdd = r % 2 == 1;
            PdfColor rowBg = (Style.StripedRows && isOdd)
                ? Style.StripedRowColor
                : Style.DefaultRowColor;
            DrawBackground(cs, tableX, y, totalW, rowH, rowBg);

            // Draw cells
            float cellX = tableX;
            for (int c = 0; c < row.Length && c < colWidths.Length; c++)
            {
                var align = ColumnAlignments != null && c < ColumnAlignments.Length
                    ? ColumnAlignments[c] : PdfAlignment.Left;

                DrawCell(cs, row[c], cellX, y, colWidths[c], rowH,
                    dataFont, fontSize, dataAlias,
                    align, PdfColor.Black, Style);

                if (Style.ShowBorder)
                    DrawBorder(cs, cellX, y, colWidths[c], rowH,
                        Style.BorderWidth, Style.BorderColor);

                cellX += colWidths[c];
            }

            ctx.Layout.Advance(rowH);
        }
    }
}
