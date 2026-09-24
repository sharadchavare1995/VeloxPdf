namespace VeloxPdf.Layout;

using VeloxPdf.Document;

public sealed class LayoutEngine
{
    public float PageWidth { get; set; }
    public float PageHeight { get; set; }
    public PdfMargins Margins { get; set; } = PdfMargins.Default;
    public float ContentWidth => PageWidth - Margins.Left - Margins.Right;
    public float ContentHeight => PageHeight - Margins.Top - Margins.Bottom;

    // CursorY is top-down: 0 = top of content area
    public float CursorY { get; private set; } = 0f;
    public float CursorX { get; private set; } = 0f;
    public int CurrentPage { get; private set; } = 1;
    public float ColumnWidth { get; private set; }
    public float ColumnX { get; private set; }

    public LayoutEngine(float pageWidth, float pageHeight, PdfMargins margins)
    {
        PageWidth = pageWidth;
        PageHeight = pageHeight;
        Margins = margins;
        ColumnWidth = ContentWidth;
        ColumnX = Margins.Left;
    }

    /// <summary>
    /// Convert top-down cursor Y (0 = top of content) to PDF coordinate Y (bottom-up).
    /// </summary>
    public float ToPdfY(float topDownY) => PageHeight - Margins.Top - topDownY;

    public float RemainingHeight => ContentHeight - CursorY;

    public bool CanFit(float height) => height <= RemainingHeight;

    public void Advance(float height)
    {
        CursorY += height;
    }

    public void AdvanceTo(float y)
    {
        CursorY = y;
    }

    public void NewPage()
    {
        CursorY = 0f;
        CursorX = 0f;
        CurrentPage++;
    }

    public void Reset()
    {
        CursorY = 0f;
        CursorX = 0f;
        CurrentPage = 1;
    }

    public void SetColumn(float x, float width)
    {
        ColumnX = x;
        ColumnWidth = width;
    }

    public void ResetColumn()
    {
        ColumnX = Margins.Left;
        ColumnWidth = ContentWidth;
    }

    // Convenience accessors for content boundaries
    public float ContentLeft => Margins.Left;
    public float ContentTop => Margins.Top;
    public float ContentRight => PageWidth - Margins.Right;
    public float ContentBottom => PageHeight - Margins.Bottom;
}
