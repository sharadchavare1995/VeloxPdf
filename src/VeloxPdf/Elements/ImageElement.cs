namespace VeloxPdf.Elements;

using VeloxPdf.Document;
using VeloxPdf.Images;
using VeloxPdf.Styling;

public sealed class ImageElement : IPdfElement
{
    public PdfImage Image { get; set; }
    public float? MaxWidth { get; set; }
    public float? MaxHeight { get; set; }
    public PdfAlignment HorizontalAlignment { get; set; } = PdfAlignment.Left;
    public bool KeepAspectRatio { get; set; } = true;
    public float SpaceBefore { get; set; } = 0f;
    public float SpaceAfter { get; set; } = 6f;

    public ImageElement(PdfImage image)
    {
        Image = image;
    }

    public bool CanSplit => false;

    private (float w, float h) CalculateDimensions(float availableWidth)
    {
        float imgW = Image.PixelWidth;
        float imgH = Image.PixelHeight;
        float maxW = MaxWidth ?? availableWidth;
        float maxH = MaxHeight ?? float.MaxValue;

        float w = Math.Min(imgW, maxW);
        float h = KeepAspectRatio ? (imgH * w / imgW) : imgH;

        if (h > maxH)
        {
            h = maxH;
            w = KeepAspectRatio ? (imgW * h / imgH) : imgW;
        }

        return (w, h);
    }

    public float MeasureHeight(RenderContext ctx)
    {
        var (_, h) = CalculateDimensions(ctx.Layout.ContentWidth);
        return h + SpaceBefore + SpaceAfter;
    }

    public void Render(RenderContext ctx)
    {
        var layout = ctx.Layout;
        string alias = ctx.CurrentPage.RegisterImage(Image);
        var (w, h) = CalculateDimensions(layout.ContentWidth);

        float x = layout.ContentLeft;
        if (HorizontalAlignment == PdfAlignment.Center)
            x = layout.ContentLeft + (layout.ContentWidth - w) / 2f;
        else if (HorizontalAlignment == PdfAlignment.Right)
            x = layout.ContentRight - w;

        layout.Advance(SpaceBefore);
        float y = layout.ToPdfY(layout.CursorY + h);

        var cs = ctx.CurrentPage.ContentStream;
        cs.SaveState();
        cs.ConcatMatrix(w, 0, 0, h, x, y);
        cs.PaintXObject(alias);
        cs.RestoreState();

        layout.Advance(h + SpaceAfter);
    }
}
