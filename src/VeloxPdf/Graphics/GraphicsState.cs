using VeloxPdf.Color;

namespace VeloxPdf.Graphics;

/// <summary>Tracks the PDF graphics state stack.</summary>
public sealed class GraphicsState
{
    private readonly Stack<GraphicsStateSnapshot> _stack = new();

    public string? CurrentFontAlias { get; set; }
    public float   CurrentFontSize  { get; set; } = 12f;
    public PdfColor FillColor   { get; set; } = PdfColor.Black;
    public PdfColor StrokeColor { get; set; } = PdfColor.Black;
    public float LineWidth  { get; set; } = 1f;
    public int   LineCap    { get; set; } = 0;
    public int   LineJoin   { get; set; } = 0;
    public float MiterLimit { get; set; } = 10f;

    public void Push()
    {
        _stack.Push(new GraphicsStateSnapshot(
            CurrentFontAlias, CurrentFontSize,
            FillColor, StrokeColor,
            LineWidth, LineCap, LineJoin, MiterLimit));
    }

    public void Pop()
    {
        if (_stack.Count > 0)
        {
            var snap = _stack.Pop();
            CurrentFontAlias = snap.FontAlias;
            CurrentFontSize  = snap.FontSize;
            FillColor        = snap.FillColor;
            StrokeColor      = snap.StrokeColor;
            LineWidth        = snap.LineWidth;
            LineCap          = snap.LineCap;
            LineJoin         = snap.LineJoin;
            MiterLimit       = snap.MiterLimit;
        }
    }

    private sealed record GraphicsStateSnapshot(
        string? FontAlias,
        float   FontSize,
        PdfColor FillColor,
        PdfColor StrokeColor,
        float LineWidth,
        int   LineCap,
        int   LineJoin,
        float MiterLimit);
}
