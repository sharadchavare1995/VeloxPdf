using VeloxPdf.Color;

namespace VeloxPdf.Graphics;

/// <summary>
/// Builds a PDF content stream using fluent PDF operator methods.
/// All methods return <c>this</c> for chaining.
/// </summary>
public sealed class ContentStream
{
    private StringBuilder _sb = new(4096);
    private readonly GraphicsState _state = new();
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    private static string F(float v) => v.ToString("0.####", Inv);

    // â”€â”€ Internal access for color objects â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    internal void AppendRaw(string s) => _sb.Append(s);

    // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    // Text operators
    // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    public ContentStream BeginText() { _sb.Append("BT\n"); return this; }
    public ContentStream EndText()   { _sb.Append("ET\n"); return this; }

    public ContentStream SetFont(string alias, float size)
    {
        _sb.Append('/').Append(alias).Append(' ').Append(F(size)).Append(" Tf\n");
        _state.CurrentFontAlias = alias;
        _state.CurrentFontSize  = size;
        return this;
    }

    public ContentStream MoveTextPos(float x, float y)
    {
        _sb.Append(F(x)).Append(' ').Append(F(y)).Append(" Td\n");
        return this;
    }

    public ContentStream SetTextMatrix(float a, float b, float c, float d, float e, float f)
    {
        _sb.Append(F(a)).Append(' ').Append(F(b)).Append(' ')
           .Append(F(c)).Append(' ').Append(F(d)).Append(' ')
           .Append(F(e)).Append(' ').Append(F(f)).Append(" Tm\n");
        return this;
    }

    public ContentStream SetAbsolutePos(float x, float y) =>
        SetTextMatrix(1, 0, 0, 1, x, y);

    public ContentStream ShowText(string text)
    {
        _sb.Append('(').Append(IO.PdfSerializer.EscapePdfString(text)).Append(") Tj\n");
        return this;
    }

    public ContentStream ShowTextNextLine(string text)
    {
        _sb.Append('(').Append(IO.PdfSerializer.EscapePdfString(text)).Append(") '\n");
        return this;
    }

    public ContentStream SetCharSpacing(float v)  { _sb.Append(F(v)).Append(" Tc\n"); return this; }
    public ContentStream SetWordSpacing(float v)   { _sb.Append(F(v)).Append(" Tw\n"); return this; }
    public ContentStream SetHorizScaling(float v)  { _sb.Append(F(v)).Append(" Tz\n"); return this; }
    public ContentStream SetLeading(float v)        { _sb.Append(F(v)).Append(" TL\n"); return this; }
    public ContentStream SetRenderMode(int v)       { _sb.Append(v).Append(" Tr\n");   return this; }
    public ContentStream NextLine()                 { _sb.Append("T*\n");              return this; }

    // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    // Path construction operators
    // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    public ContentStream MoveTo(float x, float y)
    { _sb.Append(F(x)).Append(' ').Append(F(y)).Append(" m\n"); return this; }

    public ContentStream LineTo(float x, float y)
    { _sb.Append(F(x)).Append(' ').Append(F(y)).Append(" l\n"); return this; }

    public ContentStream CurveTo(float x1, float y1, float x2, float y2, float x3, float y3)
    {
        _sb.Append(F(x1)).Append(' ').Append(F(y1)).Append(' ')
           .Append(F(x2)).Append(' ').Append(F(y2)).Append(' ')
           .Append(F(x3)).Append(' ').Append(F(y3)).Append(" c\n");
        return this;
    }

    public ContentStream ClosePath() { _sb.Append("h\n"); return this; }

    public ContentStream Rectangle(float x, float y, float w, float h)
    {
        _sb.Append(F(x)).Append(' ').Append(F(y)).Append(' ')
           .Append(F(w)).Append(' ').Append(F(h)).Append(" re\n");
        return this;
    }

    /// <summary>Draws a rectangle with rounded corners using BÃ©zier curves.</summary>
    public ContentStream RoundedRectangle(float x, float y, float w, float h, float r)
    {
        if (r <= 0f) return Rectangle(x, y, w, h);
        r = Math.Min(r, Math.Min(w / 2f, h / 2f));
        float k = 0.5523f * r;   // kappa â€“ bezier approximation constant

        MoveTo(x + r,     y);
        LineTo(x + w - r, y);
        CurveTo(x + w - r + k, y,       x + w,     y + k,       x + w,     y + r);
        LineTo(x + w,         y + h - r);
        CurveTo(x + w,        y + h - r + k, x + w - r + k, y + h, x + w - r, y + h);
        LineTo(x + r,         y + h);
        CurveTo(x + r - k,    y + h,    x,         y + h - r + k, x,         y + h - r);
        LineTo(x,             y + r);
        CurveTo(x,            y + r - k, x + r - k, y,            x + r,     y);
        ClosePath();
        return this;
    }

    // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    // Path painting operators
    // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    public ContentStream Stroke()              { _sb.Append("S\n");  return this; }
    public ContentStream CloseStroke()         { _sb.Append("s\n");  return this; }
    public ContentStream Fill()                { _sb.Append("f\n");  return this; }
    public ContentStream FillEvenOdd()         { _sb.Append("f*\n"); return this; }
    public ContentStream FillStroke()          { _sb.Append("B\n");  return this; }
    public ContentStream FillStrokeEvenOdd()   { _sb.Append("B*\n"); return this; }
    public ContentStream CloseFillStroke()     { _sb.Append("b\n");  return this; }
    public ContentStream CloseFillStrokeEvenOdd() { _sb.Append("b*\n"); return this; }
    public ContentStream EndPath()             { _sb.Append("n\n");  return this; }

    // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    // Graphics state operators
    // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    public ContentStream SaveState()
    { _state.Push(); _sb.Append("q\n"); return this; }

    public ContentStream RestoreState()
    { _state.Pop(); _sb.Append("Q\n"); return this; }

    public ContentStream SetLineWidth(float w)
    { _sb.Append(F(w)).Append(" w\n"); _state.LineWidth = w; return this; }

    public ContentStream SetLineCap(int c)
    { _sb.Append(c).Append(" J\n"); return this; }

    public ContentStream SetLineJoin(int j)
    { _sb.Append(j).Append(" j\n"); return this; }

    public ContentStream SetMiterLimit(float m)
    { _sb.Append(F(m)).Append(" M\n"); return this; }

    public ContentStream SetDash(float[] array, float phase)
    {
        _sb.Append('[');
        for (int i = 0; i < array.Length; i++)
        {
            if (i > 0) _sb.Append(' ');
            _sb.Append(F(array[i]));
        }
        _sb.Append("] ").Append(F(phase)).Append(" d\n");
        return this;
    }

    public ContentStream SetFillColor(PdfColor color)
    { color.WriteSetFill(this); return this; }

    public ContentStream SetStrokeColor(PdfColor color)
    { color.WriteSetStroke(this); return this; }

    public ContentStream SetFillColorRgb(float r, float g, float b)
    { _sb.Append(F(r)).Append(' ').Append(F(g)).Append(' ').Append(F(b)).Append(" rg\n"); return this; }

    public ContentStream SetStrokeColorRgb(float r, float g, float b)
    { _sb.Append(F(r)).Append(' ').Append(F(g)).Append(' ').Append(F(b)).Append(" RG\n"); return this; }

    public ContentStream SetFillGray(float g)
    { _sb.Append(F(g)).Append(" g\n"); return this; }

    public ContentStream SetStrokeGray(float g)
    { _sb.Append(F(g)).Append(" G\n"); return this; }

    public ContentStream ConcatMatrix(float a, float b, float c, float d, float e, float f)
    {
        _sb.Append(F(a)).Append(' ').Append(F(b)).Append(' ')
           .Append(F(c)).Append(' ').Append(F(d)).Append(' ')
           .Append(F(e)).Append(' ').Append(F(f)).Append(" cm\n");
        return this;
    }

    public ContentStream ConcatMatrix(TransformMatrix m) =>
        ConcatMatrix(m.A, m.B, m.C, m.D, m.E, m.F);

    // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    // XObject operators
    // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    public ContentStream PaintXObject(string name)
    { _sb.Append('/').Append(name).Append(" Do\n"); return this; }

    // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    // Output
    // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    /// <summary>Returns the accumulated content as Latin-1 bytes.</summary>
    public byte[] GetBytes()
    {
        int charCount = _sb.Length;
        if (charCount == 0) return Array.Empty<byte>();
        // Rent a char buffer so CopyTo can avoid allocating an intermediate string.
        // _sb.ToString() would create a full UTF-16 string copy (~12 KB per page);
        // CopyTo + GetBytes(char[]) skips that allocation entirely.
        char[] chars = ArrayPool<char>.Shared.Rent(charCount);
        try
        {
            _sb.CopyTo(0, chars, 0, charCount);
            // Latin1 maps 1:1 char→byte, so byteCount == charCount.
            byte[] result = new byte[charCount];
            Encoding.Latin1.GetBytes(chars, 0, charCount, result, 0);
            return result;
        }
        finally
        {
            ArrayPool<char>.Shared.Return(chars);
        }
    }

    /// <summary>Returns the raw content string.</summary>
    public string GetContent() => _sb.ToString();

    /// <summary>
    /// Resets the content stream, releasing the backing char[] to GC.
    /// Clear() alone only zeroes the length but keeps the char[] allocated;
    /// replacing the instance makes the old StringBuilder immediately collectible.
    /// </summary>
    public void Reset() => _sb = new StringBuilder();

    /// <summary>Gets the current character count (approximate byte count for ASCII).</summary>
    public int Length => _sb.Length;
}

