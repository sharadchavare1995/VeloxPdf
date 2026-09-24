namespace VeloxPdf.Streaming;
using VeloxPdf.Compression;
using VeloxPdf.IO;

/// <summary>
/// Renders one complete page to a compressed byte array.
/// Pipeline: acquire buffer â†’ write PDF operators for header + rows + footer â†’ compress â†’ dispose.
/// Handles stacked headers, data rows, separator lines, column dividers, and footer.
/// </summary>
public sealed class PageRenderPipeline
{
    private readonly StreamingTableSchema _schema;
    private readonly RowRenderer _renderer;
    private readonly ICompressor _compressor;

    private static readonly System.Text.Encoding Latin1 = System.Text.Encoding.Latin1;

    public PageRenderPipeline(StreamingTableSchema schema, ICompressor compressor)
    {
        _schema    = schema;
        _compressor = compressor;
        _renderer  = new RowRenderer(schema, schema.Margins.Left, schema.PageSize.Height);
    }

    /// <summary>
    /// Render a full page and return the compressed content-stream bytes.
    /// <paramref name="uncompressedSize"/> is set to the raw size before compression.
    /// </summary>
    public byte[] RenderPage(PageRenderJob job, out int uncompressedSize)
    {
        // Estimate buffer: each row â‰ˆ 80 bytes per column + overhead
        int cols = _schema.Columns.Length;
        int estimatedSize = job.RowCount * Math.Max(cols, 1) * 80 + 8192;
        var buf = new PdfBufferWriter(estimatedSize);

        float pageH     = _schema.PageSize.Height;
        float marginTop = _schema.Margins.Top;
        float marginBot = _schema.Margins.Bottom;
        float footerH   = _schema.FooterHeight;

        // Y where the header rows start (PDF Y = 0 at bottom of page)
        float headerTopY   = pageH - marginTop;
        float headerBottomY = headerTopY - _schema.TotalHeaderHeight;
        float footerBottomY = marginBot;

        Write(buf, "q\n");  // save outer graphics state

        // â”€â”€ 1. Header rows â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        if (_schema.RepeatHeadersOnEveryPage || job.PageNumber == 1)
            _renderer.RenderHeaderRows(buf, headerTopY);

        // â”€â”€ 2. Data rows and separator lines â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        float curY = headerBottomY;   // cursor moves downward (decreasing Y)
        int   oddIndex = 0;           // counts data rows for striping

        for (int r = 0; r < job.RowCount; r++)
        {
            var row = job.Rows[r];

            if (row.IsSeparator)
            {
                float sepH = RowRenderer.SeparatorHeight(row);
                curY -= sepH;
                _renderer.RenderSeparator(buf, row, curY + sepH);
            }
            else if (row.Cells != null)
            {
                bool isOdd = (oddIndex++ % 2) == 1;
                float rowH = row.Height ?? _renderer.MeasureRowHeight(row.Cells);
                curY -= rowH;
                _renderer.RenderRow(buf, row.Cells, curY, isOdd, rowH);
            }
        }

        // â”€â”€ 3. Footer â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        if (_schema.Footer != null)
            _renderer.RenderFooter(buf, job.PageNumber, footerBottomY);

        Write(buf, "Q\n");  // restore outer graphics state

        var span = buf.GetWrittenSpan();
        uncompressedSize = span.Length;
        byte[] compressed = _compressor.Compress(span);
        buf.Dispose();
        return compressed;
    }

    private static void Write(PdfBufferWriter output, string text)
    {
        byte[] bytes = Latin1.GetBytes(text);
        output.Write(bytes);
    }
}

