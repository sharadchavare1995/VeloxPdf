namespace VeloxPdf.Layout;

public sealed class PageBreakManager
{
    private readonly Dictionary<int, int> _elementStartPages = new();
    private int _nextElementId = 0;

    public int MinOrphans { get; set; } = 2;
    public int MinWidows { get; set; } = 2;

    public int RegisterElement() => _nextElementId++;

    public void RecordStart(int elementId, int pageNumber)
        => _elementStartPages[elementId] = pageNumber;

    public bool KeepTogether(float height, LayoutEngine layout)
        => height > layout.RemainingHeight;

    public bool ShouldBreakForOrphan(int linesOnCurrentPage, int totalLines)
    {
        if (linesOnCurrentPage < MinOrphans && totalLines > MinOrphans)
            return true;
        return false;
    }

    public bool ShouldBreakForWidow(int linesRemainingOnPage, int totalLines)
    {
        if (linesRemainingOnPage < MinWidows && totalLines > MinWidows)
            return true;
        return false;
    }
}
