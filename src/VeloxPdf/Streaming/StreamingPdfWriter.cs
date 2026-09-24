namespace VeloxPdf.Streaming;
using System.Diagnostics;
using VeloxPdf.Compression;
using VeloxPdf.Document;
using VeloxPdf.Primitives;

/// <summary>
/// Streaming PDF writer. Generates arbitrarily large table PDFs without loading
/// all data into memory.  Supports:
/// <list type="bullet">
/// <item>IAsyncEnumerable&lt;string[]&gt;   â€” classic simple usage</item>
/// <item>IAsyncEnumerable&lt;TableRow&gt;   â€” rich rows with separator lines</item>
/// <item>IEnumerable&lt;string[]&gt;        â€” sync source</item>
/// <item>IAsyncEnumerable&lt;T&gt; + Func  â€” typed source with projection</item>
/// </list>
/// Page breaks are driven by available vertical space so variable-height wrapped
/// rows are handled correctly.
/// </summary>
public sealed class StreamingPdfWriter : IAsyncDisposable
{
    private readonly Stream _output;
    private readonly StreamingPdfOptions _options;
    private readonly ICompressor _compressor;

    private StreamingTableSchema? _schema;
    private PdfMetadata? _metadata;
    private bool _documentBegun;
    private bool _documentEnded;

    private int _objectCounter = 0;
    private readonly List<(int ObjNum, long Offset)> _xrefEntries = [];
    private readonly List<int> _pageObjectNumbers = [];

    private int _pageTreeObjNum;
    private int _infoObjNum;
    private int _catalogObjNum;
    private int _dataFontObjNum;
    private int _headerFontObjNum;

    private long _rowsProcessed;
    private int  _pagesGenerated;
    private readonly Stopwatch _sw = new();

    private static readonly System.Text.Encoding Latin1 = System.Text.Encoding.Latin1;
    private static readonly System.Globalization.CultureInfo Inv = System.Globalization.CultureInfo.InvariantCulture;

    public StreamingPdfWriter(Stream output, StreamingPdfOptions? options = null)
    {
        if (!output.CanWrite) throw new ArgumentException("Output stream must be writable.", nameof(output));
        _output    = output;
        _options   = options ?? new StreamingPdfOptions();
        _compressor = _options.CompressPages ? new FlateCompressor() : new NoCompressor();
    }

    private int  AllocateObjectNumber() => Interlocked.Increment(ref _objectCounter);
    private void RecordXref(int objNum) => _xrefEntries.Add((objNum, _output.Position));

    // â”€â”€ BeginDocumentAsync â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    public async Task BeginDocumentAsync(
        PdfMetadata metadata,
        StreamingTableSchema schema,
        CancellationToken ct = default)
    {
        if (_documentBegun) throw new InvalidOperationException("Document already begun.");
        _documentBegun = true;
        _metadata = metadata;
        _schema   = schema;
        _sw.Start();

        _pageTreeObjNum   = AllocateObjectNumber();
        _dataFontObjNum   = AllocateObjectNumber();
        _headerFontObjNum = AllocateObjectNumber();

        long estRows  = _options.EstimatedTotalRows > 0 ? _options.EstimatedTotalRows : 1_000_000L;
        int  estPages = (int)(estRows / Math.Max(1, _options.RowsPerPage)) + 100;
        _xrefEntries.Capacity      = estPages * 2 + 50;
        _pageObjectNumbers.Capacity = estPages;

        await Task.Run(() =>
        {
            WriteRaw("%PDF-1.7\n");
            _output.WriteByte(0x25); _output.WriteByte(0xE2);
            _output.WriteByte(0xE3); _output.WriteByte(0xCF);
            _output.WriteByte(0xD3); _output.WriteByte(0x0A);

            WriteFontObject(_dataFontObjNum,   "Helvetica",      "F1");
            WriteFontObject(_headerFontObjNum, "Helvetica-Bold", "F2");
        }, ct);
    }

    private void WriteFontObject(int objNum, string baseFont, string alias)
    {
        RecordXref(objNum);
        WriteRaw($"{objNum} 0 obj\n");
        WriteRaw($"<< /Type /Font /Subtype /Type1 /BaseFont /{baseFont} /Encoding /WinAnsiEncoding >>\n");
        WriteRaw("endobj\n\n");
    }

    // â”€â”€ WriteRowsAsync â€” string[] overloads (backward compatible) â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    /// <summary>Stream rows from an async source of plain string arrays.</summary>
    public Task WriteRowsAsync(IAsyncEnumerable<string[]> rows, CancellationToken ct = default)
        => WriteTableRowsAsync(Map(rows, r => (TableRow)r), ct);

    /// <summary>Stream rows from a synchronous source.</summary>
    public Task WriteRowsAsync(IEnumerable<string[]> rows, CancellationToken ct = default)
        => WriteRowsAsync(ToAsync(rows), ct);

    /// <summary>Stream typed objects with a row projection.</summary>
    public Task WriteRowsAsync<T>(
        IAsyncEnumerable<T> source,
        Func<T, string[]> selector,
        CancellationToken ct = default)
        => WriteRowsAsync(Map(source, selector), ct);

    // â”€â”€ WriteTableRowsAsync â€” full TableRow support â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    /// <summary>Stream rich rows (data + separator lines) from an async source.</summary>
    public Task WriteTableRowsAsync(IAsyncEnumerable<TableRow> rows, CancellationToken ct = default)
        => WriteInternalAsync(rows, ct);

    /// <summary>Stream rich rows from a synchronous source.</summary>
    public Task WriteTableRowsAsync(IEnumerable<TableRow> rows, CancellationToken ct = default)
        => WriteTableRowsAsync(ToAsync(rows), ct);

    // â”€â”€ Core streaming loop â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    private async Task WriteInternalAsync(IAsyncEnumerable<TableRow> rows, CancellationToken ct)
    {
        if (_schema == null) throw new InvalidOperationException("Call BeginDocumentAsync first.");

        var pipeline      = new PageRenderPipeline(_schema, _compressor);
        float pageCapacity = _schema.ContentHeight;          // points available per page
        float usedHeight   = 0f;
        var   pageRows     = new List<TableRow>(_options.RowsPerPage > 0 ? _options.RowsPerPage : 64);
        int   dataRowCount = 0;

        // Lazy RowRenderer for height measurement (doesn't write, just measures)
        var renderer = new RowRenderer(_schema, _schema.Margins.Left, _schema.PageSize.Height);

        await foreach (var row in rows.WithCancellation(ct))
        {
            ct.ThrowIfCancellationRequested();

            // Measure how much vertical space this row needs
            float rowH;
            if (row.IsSeparator)
                rowH = RowRenderer.SeparatorHeight(row);
            else if (row.Cells != null)
                rowH = row.Height ?? renderer.MeasureRowHeight(row.Cells);
            else
                continue;

            // If it doesn't fit, flush the current page first
            if (pageRows.Count > 0 && usedHeight + rowH > pageCapacity)
            {
                await FlushPageAsync(pageRows, pipeline, ct);
                pageRows.Clear();
                usedHeight  = 0f;
                dataRowCount = 0;
            }

            pageRows.Add(row);
            usedHeight += rowH;

            if (!row.IsSeparator) { _rowsProcessed++; dataRowCount++; }

            // Also respect the explicit RowsPerPage cap if set
            bool hitRowCap = _options.RowsPerPage > 0 && dataRowCount >= _options.RowsPerPage;
            if (hitRowCap)
            {
                await FlushPageAsync(pageRows, pipeline, ct);
                pageRows.Clear();
                usedHeight   = 0f;
                dataRowCount = 0;
            }

            if (_rowsProcessed % _options.ChunkSize == 0)
                ReportProgress();
        }

        if (pageRows.Count > 0)
            await FlushPageAsync(pageRows, pipeline, ct);
    }

    private async Task FlushPageAsync(
        List<TableRow> rows, PageRenderPipeline pipeline, CancellationToken ct)
    {
        int pageObjNum    = AllocateObjectNumber();
        int contentObjNum = AllocateObjectNumber();

        var job = new PageRenderJob
        {
            PageNumber          = _pagesGenerated + 1,
            PageObjectNumber    = pageObjNum,
            ContentObjectNumber = contentObjNum,
            Rows                = rows.ToArray(),
            IsFirstPage         = _pagesGenerated == 0
        };

        byte[] compressed = pipeline.RenderPage(job, out _);
        string? filterEntry = _options.CompressPages ? " /Filter /FlateDecode" : "";
        float pw = _schema!.PageSize.Width;
        float ph = _schema.PageSize.Height;

        await Task.Run(() =>
        {
            // Content stream object
            RecordXref(contentObjNum);
            WriteRaw($"{contentObjNum} 0 obj\n");
            WriteRaw($"<< /Length {compressed.Length}{filterEntry} >>\n");
            WriteRaw("stream\n");
            _output.Write(compressed, 0, compressed.Length);
            WriteRaw("\nendstream\nendobj\n\n");

            // Page dictionary object
            RecordXref(pageObjNum);
            WriteRaw($"{pageObjNum} 0 obj\n");
            WriteRaw("<< /Type /Page\n");
            WriteRaw($"   /Parent {_pageTreeObjNum} 0 R\n");
            WriteRaw($"   /MediaBox [0 0 {F(pw)} {F(ph)}]\n");
            WriteRaw($"   /Contents {contentObjNum} 0 R\n");
            WriteRaw($"   /Resources << /Font << /F1 {_dataFontObjNum} 0 R /F2 {_headerFontObjNum} 0 R >>" +
                     " /ProcSet [/PDF /Text] >> >>\n");
            WriteRaw(">>\nendobj\n\n");

            _pageObjectNumbers.Add(pageObjNum);
        }, ct);

        _pagesGenerated++;
    }

    // â”€â”€ EndDocumentAsync â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    public async Task EndDocumentAsync(CancellationToken ct = default)
    {
        if (_documentEnded) return;
        _documentEnded = true;

        await Task.Run(() =>
        {
            // Pages tree
            RecordXref(_pageTreeObjNum);
            WriteRaw($"{_pageTreeObjNum} 0 obj\n");
            WriteRaw("<< /Type /Pages\n");
            WriteRaw($"   /Count {_pageObjectNumbers.Count}\n");
            WriteRaw("   /Kids [");
            for (int i = 0; i < _pageObjectNumbers.Count; i++)
            {
                if (i > 0) WriteRaw(" ");
                WriteRaw($"{_pageObjectNumbers[i]} 0 R");
            }
            WriteRaw("]\n>>\nendobj\n\n");

            // Info dictionary
            _infoObjNum = AllocateObjectNumber();
            RecordXref(_infoObjNum);
            WriteRaw($"{_infoObjNum} 0 obj\n");
            WritePdfDict(_metadata ?? new PdfMetadata());
            WriteRaw("\nendobj\n\n");

            // Catalog
            _catalogObjNum = AllocateObjectNumber();
            RecordXref(_catalogObjNum);
            WriteRaw($"{_catalogObjNum} 0 obj\n");
            WriteRaw($"<< /Type /Catalog /Pages {_pageTreeObjNum} 0 R >>\nendobj\n\n");

            // Cross-reference table
            long xrefOffset = _output.Position;
            int  total      = _objectCounter;
            var  sorted     = _xrefEntries.OrderBy(e => e.ObjNum).ToList();

            WriteRaw("xref\n");
            WriteRaw($"0 {total + 1}\n");
            WriteRaw("0000000000 65535 f \n");

            int next = 1;
            foreach (var (obj, off) in sorted)
            {
                while (next < obj) { WriteRaw("0000000000 00000 f \n"); next++; }
                WriteRaw($"{off:D10} 00000 n \n");
                next = obj + 1;
            }

            WriteRaw("trailer\n");
            WriteRaw($"<< /Size {total + 1} /Root {_catalogObjNum} 0 R /Info {_infoObjNum} 0 R >>\n");
            WriteRaw("startxref\n");
            WriteRaw($"{xrefOffset}\n");
            WriteRaw("%%EOF\n");
            _output.Flush();
        }, ct);

        _sw.Stop();
        ReportProgress(final: true);
    }

    private void WritePdfDict(PdfMetadata info)
    {
        var sb = new System.Text.StringBuilder();
        sb.Append("<< ");
        void Add(string key, string? val)
        {
            if (string.IsNullOrEmpty(val)) return;
            sb.Append('/').Append(key).Append(" (")
              .Append(IO.PdfSerializer.EscapePdfString(val)).Append(") ");
        }
        Add("Title",        info.Title);
        Add("Author",       info.Author);
        Add("Subject",      info.Subject);
        Add("Creator",      info.Creator ?? "VeloxPdf");
        Add("Producer",     "VeloxPdf 1.0");
        Add("CreationDate", $"D:{DateTime.UtcNow:yyyyMMddHHmmss}Z");
        sb.Append(">>");
        WriteRaw(sb.ToString());
    }

    private void ReportProgress(bool final = false)
    {
        if (_options.OnProgress == null) return;
        double sec = _sw.Elapsed.TotalSeconds;
        TimeSpan? remaining = null;
        if (_options.EstimatedTotalRows > 0 && _rowsProcessed > 0 && sec > 0.1)
        {
            double rps = _rowsProcessed / sec;
            if (rps > 0)
                remaining = TimeSpan.FromSeconds((_options.EstimatedTotalRows - _rowsProcessed) / rps);
        }
        _options.OnProgress(new StreamingProgress
        {
            RowsProcessed      = _rowsProcessed,
            TotalRows          = _options.EstimatedTotalRows,
            PagesGenerated     = _pagesGenerated,
            Elapsed            = _sw.Elapsed,
            EstimatedRemaining = remaining,
            MemoryUsedBytes    = GC.GetTotalMemory(false)
        });
    }

    private void WriteRaw(string text)
    {
        byte[] b = Latin1.GetBytes(text);
        _output.Write(b, 0, b.Length);
    }

    private static string F(float v) => v.ToString("0.####", Inv);

    public async ValueTask DisposeAsync()
    {
        if (_documentBegun && !_documentEnded)
            await EndDocumentAsync();
    }

    // â”€â”€ Helpers â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    private static async IAsyncEnumerable<T> ToAsync<T>(IEnumerable<T> source)
    {
        foreach (var item in source) { yield return item; await Task.Yield(); }
    }

    private static async IAsyncEnumerable<TOut> Map<TIn, TOut>(
        IAsyncEnumerable<TIn> source, Func<TIn, TOut> selector)
    {
        await foreach (var item in source) yield return selector(item);
    }
}

