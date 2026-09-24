namespace VeloxPdf.Elements;

using VeloxPdf.Document;

public sealed class PageBreakElement : IPdfElement
{
    public bool CanSplit => false;

    public void Render(RenderContext ctx)
    {
        ctx.RequestNewPage();
    }

    public float MeasureHeight(RenderContext ctx) => ctx.Layout.RemainingHeight;
}
