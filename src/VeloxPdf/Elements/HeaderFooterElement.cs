namespace VeloxPdf.Elements;

using VeloxPdf.Document;

public sealed class HeaderFooterElement : IPdfElement
{
    public Action<RenderContext, int, int>? Action { get; set; }
    public bool IsHeader { get; set; } = true;
    public float Height { get; set; } = 20f;
    public bool CanSplit => false;

    public float MeasureHeight(RenderContext ctx) => 0f;

    public void Render(RenderContext ctx)
    {
        Action?.Invoke(ctx, ctx.Layout.CurrentPage, 0);
    }
}
