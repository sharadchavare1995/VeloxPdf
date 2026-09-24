namespace VeloxPdf.Elements;

using VeloxPdf.Document;

public interface IPdfElement
{
    void Render(RenderContext ctx);
    float MeasureHeight(RenderContext ctx);
    bool CanSplit { get; }
}
