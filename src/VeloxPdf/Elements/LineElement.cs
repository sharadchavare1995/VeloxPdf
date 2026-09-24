namespace VeloxPdf.Elements;

using VeloxPdf.Color;
using VeloxPdf.Document;

public sealed class LineElement : IPdfElement
{
    public PdfColor Color { get; set; } = PdfColor.Black;
    public float LineWidth { get; set; } = 0.5f;
    public float[]? DashPattern { get; set; }
    public float SpaceBefore { get; set; } = 6f;
    public float SpaceAfter { get; set; } = 6f;
    public bool CanSplit => false;

    public float MeasureHeight(RenderContext ctx) => SpaceBefore + LineWidth + SpaceAfter;

    public void Render(RenderContext ctx)
    {
        var cs = ctx.CurrentPage.ContentStream;
        var layout = ctx.Layout;

        float y = layout.ToPdfY(layout.CursorY + SpaceBefore);
        float x1 = layout.ContentLeft;
        float x2 = layout.ContentRight;

        cs.SaveState();
        cs.SetStrokeColor(Color);
        cs.SetLineWidth(LineWidth);
        if (DashPattern != null && DashPattern.Length > 0)
            cs.SetDash(DashPattern, 0);
        cs.MoveTo(x1, y);
        cs.LineTo(x2, y);
        cs.Stroke();
        cs.RestoreState();

        layout.Advance(MeasureHeight(ctx));
    }
}
