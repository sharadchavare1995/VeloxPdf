namespace VeloxPdf.Streaming;
using VeloxPdf.Color;
using VeloxPdf.Fonts;
using VeloxPdf.IO;

/// <summary>
/// High-performance row renderer.
/// Supports: multi-level stacked headers, column dividers, wrapped and trimmed cell text,
/// single/double/thick separator lines, and a footer with page number + print date.
///
/// Pre-encodes all constant byte sequences once at construction time so the hot
/// per-row path does zero string allocations for fixed content.
/// </summary>
public sealed class RowRenderer
{
    private readonly StreamingTableSchema _schema;
    private readonly float[] _colX;   // absolute X of left edge of each column
    private readonly float[] _colW;   // absolute width of each column
    private readonly float _tableLeft;
    private readonly float _tableRight;
    private readonly float _pageHeight;
    private readonly float _dataFontSize;
    private readonly string _printDate;

    // Pre-encoded constant byte sequences
    private readonly byte[] _btBytes;
    private readonly byte[] _etBytes;
    private readonly byte[] _saveBytes;
    private readonly byte[] _restoreBytes;

    // Colors as pre-encoded rg/RG strings
    private readonly string _strokeColorStr;
    private readonly string _borderWidthStr;
    private readonly string _evenRowBgStr;
    private readonly string _oddRowBgStr;
    private readonly string _footerTextColorStr;

    private static readonly System.Text.Encoding  Latin1 = System.Text.Encoding.Latin1;
    private static readonly System.Globalization.CultureInfo Inv = System.Globalization.CultureInfo.InvariantCulture;

    // Font references (static; no PdfPage needed for metrics)
    private static readonly Type1Font DataFont   = Type1Font.Helvetica;
    private static readonly Type1Font BoldFont   = Type1Font.HelveticaBold;

    public RowRenderer(StreamingTableSchema schema, float tableLeft, float pageHeight)
    {
        _schema      = schema;
        _tableLeft   = tableLeft;
        _pageHeight  = pageHeight;
        _dataFontSize = schema.DataFontSize;
        _printDate   = DateTime.Now.ToString(
            schema.Footer?.DateFormat ?? "yyyy-MM-dd HH:mm", Inv);

        float contentW = schema.ContentWidth;
        _colW = schema.GetColumnWidths(contentW);
        _colX = new float[_colW.Length];
        if (_colX.Length > 0)
        {
            _colX[0] = tableLeft;
            for (int i = 1; i < _colX.Length; i++)
                _colX[i] = _colX[i - 1] + _colW[i - 1];
        }
        _tableRight = tableLeft + contentW;

        // Pre-encode universal constants
        _btBytes      = Latin1.GetBytes("BT\n");
        _etBytes      = Latin1.GetBytes("ET\n");
        _saveBytes    = Latin1.GetBytes("q\n");
        _restoreBytes = Latin1.GetBytes("Q\n");

        // Style-derived strings
        var st = schema.Style;
        _strokeColorStr  = ColorToFillStr(st.BorderColor)  .Replace(" rg\n", " RG\n");
        _borderWidthStr  = $"{F(st.BorderWidth)} w\n";
        _evenRowBgStr    = ColorToFillStr(st.DefaultRowColor);
        _oddRowBgStr     = ColorToFillStr(st.StripedRowColor);
        _footerTextColorStr = schema.Footer?.TextColor is RgbColor fc
            ? $"{F(fc.R)} {F(fc.G)} {F(fc.B)} rg\n"
            : "0.4196 0.4471 0.502 rg\n"; // #6B7280
    }

    // â”€â”€ Public height API â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    /// <summary>
    /// Measure the height a data row will occupy, accounting for text wrapping.
    /// For Trim/Clip columns the height is always one line; for Wrap columns it
    /// expands to the maximum number of wrapped lines across all columns.
    /// </summary>
    public float MeasureRowHeight(string[] cells)
    {
        float lineH = _dataFontSize * 1.2f;
        int   maxLines = 1;
        float padV = _schema.Style.CellPaddingTop + _schema.Style.CellPaddingBottom;

        for (int c = 0; c < cells.Length && c < _schema.Columns.Length; c++)
        {
            var col = _schema.Columns[c];
            if (col.Wrap != CellWrapMode.Wrap) continue;
            float fs = col.FontSize ?? _dataFontSize;
            float avail = _colW[c] - _schema.Style.CellPaddingLeft - _schema.Style.CellPaddingRight;
            int lines = WrapLines(cells[c], avail, fs, DataFont).Length;
            maxLines = Math.Max(maxLines, lines);
        }
        return Math.Max(_schema.Style.RowMinHeight, maxLines * lineH + padV);
    }

    /// <summary>Height of a separator line (fixed).</summary>
    public static float SeparatorHeight(TableRow sep)
        => sep.Height ?? (sep.SeparatorStyle == LineStyle.Double ? 10f : 6f);

    // â”€â”€ Rendering â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    /// <summary>Render all stacked header rows starting at <paramref name="topY"/> (PDF Y, bottom-up).</summary>
    public void RenderHeaderRows(PdfBufferWriter buf, float topY)
    {
        var headerRows = _schema.EffectiveHeaderRows;
        float y = topY;
        float contentW = _tableRight - _tableLeft;

        for (int level = 0; level < headerRows.Length; level++)
        {
            var hRow = headerRows[level];
            float h = hRow.Height;
            float rowBottomY = y - h;

            // Optional top border
            if (hRow.ShowTopBorder)
                DrawHLine(buf, _tableLeft, y, _tableRight, _borderWidthStr, _strokeColorStr);

            // Background fill
            string bgStr = hRow.Background is RgbColor rbg
                ? $"{F(rbg.R)} {F(rbg.G)} {F(rbg.B)} rg\n"
                : ColorToFillStr(_schema.Style.HeaderBackgroundColor);
            DrawFilledRect(buf, _tableLeft, rowBottomY, contentW, h, bgStr);

            // Header text
            string textColorStr = hRow.TextColor is RgbColor rtc
                ? $"{F(rtc.R)} {F(rtc.G)} {F(rtc.B)} rg\n"
                : "1 1 1 rg\n"; // white default
            string fontKey = hRow.Bold ? "F2" : "F1";
            float  fs      = hRow.FontSize;

            buf.Write(_btBytes);
            Write(buf, $"/{fontKey} {F(fs)} Tf\n");
            Write(buf, textColorStr);

            for (int c = 0; c < _schema.Columns.Length && c < _colX.Length; c++)
            {
                var col = _schema.Columns[c];
                string cellText = (col.Headers.Length > level) ? col.Headers[level] : "";
                if (string.IsNullOrEmpty(cellText)) continue;

                CellAlign align = hRow.ColumnAlignments != null && c < hRow.ColumnAlignments.Length
                    ? hRow.ColumnAlignments[c]
                    : CellAlign.Left;

                float tx = AlignedX(cellText, c, fs, hRow.Bold ? BoldFont : DataFont, align);
                float ty = rowBottomY + _schema.Style.CellPaddingBottom;

                WriteTm(buf, tx, ty);
                WriteTjClipped(buf, cellText, _colW[c] - _schema.Style.CellPaddingLeft - _schema.Style.CellPaddingRight, fs, hRow.Bold ? BoldFont : DataFont);
            }
            buf.Write(_etBytes);

            // Column dividers within header
            if (_schema.ShowColumnDividers)
                DrawColumnDividers(buf, rowBottomY, y, textColorStr);

            // Optional bottom border
            if (hRow.ShowBottomBorder)
                DrawHLine(buf, _tableLeft, rowBottomY, _tableRight, _borderWidthStr, _strokeColorStr);

            y = rowBottomY;
        }
    }

    /// <summary>
    /// Render one data row. Returns the actual height used (varies when any column wraps).
    /// </summary>
    public float RenderRow(PdfBufferWriter buf, string[] cells, float rowBottomY, bool isOdd, float? fixedHeight = null)
    {
        float rowH = fixedHeight ?? MeasureRowHeight(cells);
        float rowTopY = rowBottomY + rowH;
        float contentW = _tableRight - _tableLeft;

        // Background fill
        string bgStr = (isOdd && _schema.Style.StripedRows) ? _oddRowBgStr : _evenRowBgStr;
        DrawFilledRect(buf, _tableLeft, rowBottomY, contentW, rowH, bgStr);

        // Cell text
        buf.Write(_btBytes);
        Write(buf, $"/F1 {F(_dataFontSize)} Tf\n");
        Write(buf, "0 0 0 rg\n");

        for (int c = 0; c < cells.Length && c < _schema.Columns.Length; c++)
        {
            var col = _schema.Columns[c];
            float fs = col.FontSize ?? _dataFontSize;
            float avail = _colW[c] - _schema.Style.CellPaddingLeft - _schema.Style.CellPaddingRight;
            float lineH = fs * 1.2f;

            // Switch font if column has a different font size
            if (col.FontSize.HasValue)
                Write(buf, $"/F1 {F(fs)} Tf\n");

            if (col.Wrap == CellWrapMode.Wrap)
            {
                // Multi-line: render each wrapped line
                string[] lines = WrapLines(cells[c], avail, fs, DataFont);
                // Top-aligned within the cell
                float lineY = rowTopY - _schema.Style.CellPaddingTop - fs;
                foreach (string line in lines)
                {
                    float tx = AlignedX(line, c, fs, DataFont, col.Align);
                    WriteTm(buf, tx, lineY);
                    WriteTj(buf, line);
                    lineY -= lineH;
                    if (lineY < rowBottomY) break;
                }
            }
            else
            {
                // Single line: trim or clip
                string text = col.Wrap == CellWrapMode.Trim
                    ? TrimText(cells[c], avail, fs, DataFont)
                    : cells[c]; // Clip: PDF clipping handles it

                float tx = AlignedX(text, c, fs, DataFont, col.Align);
                float ty = rowBottomY + _schema.Style.CellPaddingBottom;

                if (col.Wrap == CellWrapMode.Clip)
                {
                    // Use PDF clipping path so text can't bleed into neighbour
                    Write(buf, "ET\n");
                    SetClipRect(buf, _colX[c], rowBottomY, _colW[c], rowH);
                    buf.Write(_btBytes);
                    Write(buf, $"/F1 {F(fs)} Tf\n");
                    Write(buf, "0 0 0 rg\n");
                }

                WriteTm(buf, tx, ty);
                WriteTj(buf, text);
            }

            // Restore column font size if it was overridden
            if (col.FontSize.HasValue)
                Write(buf, $"/F1 {F(_dataFontSize)} Tf\n");
        }
        buf.Write(_etBytes);

        // Bottom row border
        if (_schema.Style.ShowBorder)
            DrawHLine(buf, _tableLeft, rowBottomY, _tableRight, _borderWidthStr, _strokeColorStr);

        // Vertical column dividers
        if (_schema.ShowColumnDividers)
            DrawColumnDividers(buf, rowBottomY, rowTopY, _strokeColorStr);

        return rowH;
    }

    /// <summary>Render a horizontal separator line (single, double, or thick).</summary>
    public float RenderSeparator(PdfBufferWriter buf, TableRow sep, float topY)
    {
        float totalH = SeparatorHeight(sep);
        float lineY  = topY - totalH / 2f; // Centre the line(s) vertically in the space
        float lw     = _schema.Style.BorderWidth;

        buf.Write(_saveBytes);
        Write(buf, _strokeColorStr);

        switch (sep.SeparatorStyle)
        {
            case LineStyle.Double:
                Write(buf, $"{F(lw)} w\n");
                DrawRawHLine(buf, _tableLeft, lineY + 1.25f, _tableRight);
                DrawRawHLine(buf, _tableLeft, lineY - 1.25f, _tableRight);
                break;
            case LineStyle.Thick:
                Write(buf, $"{F(lw * 2.5f)} w\n");
                DrawRawHLine(buf, _tableLeft, lineY, _tableRight);
                break;
            default: // Single
                Write(buf, $"{F(lw)} w\n");
                DrawRawHLine(buf, _tableLeft, lineY, _tableRight);
                break;
        }

        buf.Write(_restoreBytes);
        return totalH;
    }

    /// <summary>
    /// Render the footer at the very bottom of the page.
    /// <paramref name="footerBottomY"/> = Margins.Bottom (PDF coordinate).
    /// </summary>
    public void RenderFooter(PdfBufferWriter buf, int pageNumber, float footerBottomY)
    {
        var footer = _schema.Footer;
        if (footer == null) return;

        float h          = footer.Height;
        float contentW   = _tableRight - _tableLeft;
        float fs         = footer.FontSize;
        string dateToken = _printDate;
        string pageToken = pageNumber.ToString();

        // Optional background
        if (footer.Background is RgbColor fbg)
        {
            DrawFilledRect(buf, _tableLeft, footerBottomY, contentW, h,
                $"{F(fbg.R)} {F(fbg.G)} {F(fbg.B)} rg\n");
        }

        // Top border of footer
        if (footer.ShowTopBorder)
        {
            string bw = footer.TopBorderWidth > 0 ? $"{F(footer.TopBorderWidth)} w\n" : _borderWidthStr;
            string bc = footer.TopBorderColor is RgbColor bc2
                ? $"{F(bc2.R)} {F(bc2.G)} {F(bc2.B)} RG\n"
                : _strokeColorStr;
            DrawHLine(buf, _tableLeft, footerBottomY + h, _tableRight, bw, bc);
        }

        // Text baseline
        float ty = footerBottomY + (h - fs) / 2f;

        buf.Write(_btBytes);
        Write(buf, $"/F1 {F(fs)} Tf\n");
        Write(buf, _footerTextColorStr);

        // Resolve all three slots
        string left   = Resolve(footer.LeftText   ?? (footer.ShowPrintDate  ? footer.PrintDateFormat : ""), pageToken, dateToken);
        string centre = Resolve(footer.CenterText  ?? "", pageToken, dateToken);
        string right  = Resolve(footer.RightText   ?? (footer.ShowPageNumber ? footer.PageNumberFormat : ""), pageToken, dateToken);

        // Left
        if (!string.IsNullOrEmpty(left))
        {
            WriteTm(buf, _tableLeft + _schema.Style.CellPaddingLeft, ty);
            WriteTj(buf, left);
        }

        // Centre
        if (!string.IsNullOrEmpty(centre))
        {
            float tw = DataFont.MeasureWidth(centre.AsSpan(), fs);
            float cx = _tableLeft + (contentW - tw) / 2f;
            WriteTm(buf, cx, ty);
            WriteTj(buf, centre);
        }

        // Right
        if (!string.IsNullOrEmpty(right))
        {
            float tw = DataFont.MeasureWidth(right.AsSpan(), fs);
            float rx = _tableRight - tw - _schema.Style.CellPaddingRight;
            WriteTm(buf, rx, ty);
            WriteTj(buf, right);
        }

        buf.Write(_etBytes);
    }

    // â”€â”€ Text measurement & wrapping â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    /// <summary>
    /// Break text into lines that fit within <paramref name="availWidth"/> at the given font size.
    /// Words that are wider than the column are hard-broken at the character boundary.
    /// </summary>
    public static string[] WrapLines(string text, float availWidth, float fontSize, Type1Font font)
    {
        if (string.IsNullOrEmpty(text)) return [text];
        if (font.MeasureWidth(text.AsSpan(), fontSize) <= availWidth) return [text];

        var lines = new System.Collections.Generic.List<string>(4);
        var buf   = new System.Text.StringBuilder(text.Length);
        float curW = 0f;
        float spW  = font.MeasureWidth(" ".AsSpan(), fontSize);

        string[] words = text.Split(' ');
        foreach (string word in words)
        {
            if (string.IsNullOrEmpty(word)) continue;
            float wordW = font.MeasureWidth(word.AsSpan(), fontSize);

            if (buf.Length == 0)
            {
                // Start of a line: hard-break if the word itself is too wide
                if (wordW > availWidth)
                {
                    AppendHardBroken(word, availWidth, fontSize, font, lines, buf, ref curW);
                }
                else
                {
                    buf.Append(word);
                    curW = wordW;
                }
            }
            else if (curW + spW + wordW <= availWidth)
            {
                buf.Append(' ').Append(word);
                curW += spW + wordW;
            }
            else
            {
                // Flush current line, start a new one
                lines.Add(buf.ToString());
                buf.Clear();
                if (wordW > availWidth)
                    AppendHardBroken(word, availWidth, fontSize, font, lines, buf, ref curW);
                else { buf.Append(word); curW = wordW; }
            }
        }
        if (buf.Length > 0) lines.Add(buf.ToString());
        return lines.Count == 0 ? [text] : lines.ToArray();
    }

    private static void AppendHardBroken(
        string word, float availWidth, float fontSize, Type1Font font,
        System.Collections.Generic.List<string> lines,
        System.Text.StringBuilder buf, ref float curW)
    {
        foreach (char ch in word)
        {
            float cw = font.MeasureWidth(ch, fontSize);
            if (curW + cw > availWidth && buf.Length > 0)
            {
                lines.Add(buf.ToString());
                buf.Clear();
                curW = 0f;
            }
            buf.Append(ch);
            curW += cw;
        }
    }

    /// <summary>
    /// Trim text to fit within <paramref name="availWidth"/>, appending "..." when truncated.
    /// Never returns text wider than <paramref name="availWidth"/>.
    /// </summary>
    public static string TrimText(string text, float availWidth, float fontSize, Type1Font font)
    {
        if (string.IsNullOrEmpty(text)) return text;
        if (font.MeasureWidth(text.AsSpan(), fontSize) <= availWidth) return text;

        const string Ellipsis = "...";
        float ellW = font.MeasureWidth(Ellipsis.AsSpan(), fontSize);
        float target = availWidth - ellW;

        if (target <= 0f) return Ellipsis;

        // Binary search for the largest prefix that fits
        int lo = 0, hi = text.Length;
        while (lo < hi)
        {
            int mid = (lo + hi + 1) / 2;
            if (font.MeasureWidth(text.AsSpan(0, mid), fontSize) <= target) lo = mid;
            else hi = mid - 1;
        }
        return lo > 0 ? text[..lo] + Ellipsis : Ellipsis;
    }

    // â”€â”€ Alignment helpers â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    private float AlignedX(string text, int colIndex, float fontSize, Type1Font font, CellAlign align)
    {
        float leftEdge   = _colX[colIndex] + _schema.Style.CellPaddingLeft;
        float rightEdge  = _colX[colIndex] + _colW[colIndex] - _schema.Style.CellPaddingRight;
        float availWidth = rightEdge - leftEdge;

        return align switch
        {
            CellAlign.Right  => rightEdge - font.MeasureWidth(text.AsSpan(), fontSize),
            CellAlign.Center => leftEdge + (availWidth - font.MeasureWidth(text.AsSpan(), fontSize)) / 2f,
            _                => leftEdge
        };
    }

    // â”€â”€ Low-level PDF operators â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    private void DrawColumnDividers(PdfBufferWriter buf, float bottomY, float topY, string strokeStr)
    {
        // Skip the first column's left edge; only draw right edges
        for (int i = 0; i < _colX.Length - 1; i++)
        {
            float divX = _colX[i] + _colW[i];
            buf.Write(_saveBytes);
            Write(buf, strokeStr);
            Write(buf, _borderWidthStr);
            Write(buf, $"{F(divX)} {F(bottomY)} m\n{F(divX)} {F(topY)} l\nS\n");
            buf.Write(_restoreBytes);
        }
    }

    private void DrawHLine(PdfBufferWriter buf, float x1, float y, float x2, string lw, string colorStr)
    {
        buf.Write(_saveBytes);
        Write(buf, colorStr);
        Write(buf, lw);
        DrawRawHLine(buf, x1, y, x2);
        buf.Write(_restoreBytes);
    }

    private static void DrawRawHLine(PdfBufferWriter buf, float x1, float y, float x2)
        => Write(buf, $"{F(x1)} {F(y)} m\n{F(x2)} {F(y)} l\nS\n");

    private static void DrawFilledRect(PdfBufferWriter buf, float x, float y, float w, float h, string colorStr)
    {
        Write(buf, "q\n");
        Write(buf, colorStr);
        Write(buf, $"{F(x)} {F(y)} {F(w)} {F(h)} re f\n");
        Write(buf, "Q\n");
    }

    private static void SetClipRect(PdfBufferWriter buf, float x, float y, float w, float h)
    {
        Write(buf, "q\n");
        Write(buf, $"{F(x)} {F(y)} {F(w)} {F(h)} re W n\n");
    }

    private static void WriteTm(PdfBufferWriter buf, float x, float y)
        => Write(buf, $"1 0 0 1 {F(x)} {F(y)} Tm\n");

    private static void WriteTj(PdfBufferWriter buf, string text)
    {
        Write(buf, "(");
        WriteEscaped(buf, text);
        Write(buf, ") Tj\n");
    }

    /// <summary>Write text, trimming to fit (used for header cells).</summary>
    private void WriteTjClipped(PdfBufferWriter buf, string text, float avail, float fs, Type1Font font)
    {
        string t = TrimText(text, avail, fs, font);
        WriteTj(buf, t);
    }

    private static void WriteEscaped(PdfBufferWriter buf, string text)
    {
        bool hasSpecial = false;
        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            if (c == '(' || c == ')' || c == '\\' || c == '\r' || c == '\n')
                { hasSpecial = true; break; }
        }
        if (!hasSpecial) { Write(buf, text); return; }

        var sb = new System.Text.StringBuilder(text.Length + 8);
        foreach (char c in text)
        {
            switch (c)
            {
                case '(':  sb.Append("\\("); break;
                case ')':  sb.Append("\\)"); break;
                case '\\': sb.Append("\\\\"); break;
                case '\r': sb.Append("\\r"); break;
                case '\n': sb.Append("\\n"); break;
                default:   sb.Append(c); break;
            }
        }
        Write(buf, sb.ToString());
    }

    // â”€â”€ Utilities â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    private static string Resolve(string template, string pageNum, string date)
        => template.Replace("{n}", pageNum).Replace("{d}", date);

    private static string ColorToFillStr(PdfColor color)
    {
        if (color is RgbColor rgb)
            return $"{F(rgb.R)} {F(rgb.G)} {F(rgb.B)} rg\n";
        return "0 0 0 rg\n"; // fallback black
    }

    private static string F(float v) => v.ToString("0.####", Inv);

    private static void Write(PdfBufferWriter output, string text)
    {
        byte[] bytes = Latin1.GetBytes(text);
        output.Write(bytes);
    }

    // â”€â”€ Public property accessors â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    public float TableLeft  => _tableLeft;
    public float TableRight => _tableRight;
}

