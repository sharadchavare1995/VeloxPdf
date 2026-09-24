namespace VeloxPdf.Streaming;

/// <summary>Carries one page's worth of rows plus metadata to the render pipeline.</summary>
public sealed class PageRenderJob
{
    public int PageNumber          { get; init; }
    public int PageObjectNumber    { get; init; }
    public int ContentObjectNumber { get; init; }

    /// <summary>All rows for this page — both data rows and separator lines.</summary>
    public TableRow[] Rows         { get; init; } = [];

    /// <summary>Number of rows (convenience shorthand).</summary>
    public int RowCount            => Rows.Length;

    public bool IsFirstPage        { get; init; }
    public bool IsLastPage         { get; set; }
}
