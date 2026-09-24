namespace VeloxPdf.Document;
using VeloxPdf.Fonts;
using VeloxPdf.Layout;

public sealed class RenderContext
{
    public PdfPage CurrentPage { get; private set; }
    public LayoutEngine Layout { get; }
    public PdfFont DefaultFont { get; set; }
    public float DefaultFontSize { get; set; }
    public PdfDocument Document { get; }

    private readonly List<PdfPage> _pages;
    private bool _newPageRequested;

    public RenderContext(PdfDocument document, PdfPage firstPage,
        LayoutEngine layout, PdfFont defaultFont, float defaultFontSize)
    {
        Document = document;
        CurrentPage = firstPage;
        Layout = layout;
        DefaultFont = defaultFont;
        DefaultFontSize = defaultFontSize;
        _pages = document.Pages;
    }

    public void RequestNewPage()
    {
        if (_newPageRequested) return;
        _newPageRequested = true;
        AddPage();
    }

    public void AddPage()
    {
        _newPageRequested = false;
        var newPage = Document.AddPage(
            new PdfPageSize(CurrentPage.Width, CurrentPage.Height),
            CurrentPage.Margins);
        CurrentPage = newPage;
        Layout.NewPage();
    }

    public bool WasNewPageRequested => _newPageRequested;
    public void ClearNewPageRequest() => _newPageRequested = false;
}
