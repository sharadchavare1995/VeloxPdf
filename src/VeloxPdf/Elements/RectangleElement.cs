namespace VeloxPdf.Elements;

using VeloxPdf.Color;
using VeloxPdf.Document;

public sealed class RectangleElement : IPdfElement
{
    public float? Width { get; set; }   // null = full content width
    public float Height { get; set; } = 20f;
    public PdfColor? FillColor { get; set; }
    public PdfColor? StrokeColor { get; set; }
    public float BorderWidth { get; set; } = 0.5f;
    public float CornerRadius { get; set; } = 0f;
    public bool CanSplit => false;

    public float MeasureHeight(RenderContext ctx) => Height;

    public void Render(RenderContext ctx)
    {
        var cs = ctx.CurrentPage.ContentStream;
        var layout = ctx.Layout;

        float x = layout.ContentLeft;
        float y = layout.ToPdfY(layout.CursorY + Height);
        float w = Width ?? layout.ContentWidth;
        float h = Height;

        cs.SaveState();

        if (CornerRadius > 0)
        {
            cs.RoundedRectangle(x, y, w, h, CornerRadius);
        }
        else
        {
            cs.Rectangle(x, y, w, h);
        }

        if (FillColor != null && StrokeColor != null)
        {
            cs.SetFillColor(FillColor);
            cs.SetStrokeColor(StrokeColor);
            cs.SetLineWidth(BorderWidth);
            cs.FillStroke();
        }
        else if (FillColor != null)
        {
            cs.SetFillColor(FillColor);
            cs.Fill();
        }
        else if (StrokeColor != null)
        {
            cs.SetStrokeColor(StrokeColor);
            cs.SetLineWidth(BorderWidth);
            cs.Stroke();
        }

        cs.RestoreState();
        layout.Advance(Height);
    }
}
