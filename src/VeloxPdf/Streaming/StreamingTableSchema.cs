namespace VeloxPdf.Streaming;
using VeloxPdf.Document;
using VeloxPdf.Styling;

/// <summary>
/// Defines everything about a streaming table: columns (width, wrap, alignment, headers),
/// stacked multi-row header definitions, footer, styling, and page layout.
/// </summary>
public sealed class StreamingTableSchema
{
    // ── Column definitions ───────────────────────────────────────────────────

    /// <summary>
    /// Full per-column definitions. Each entry specifies width, text wrap behaviour,
    /// alignment, and the header text per stacked header level.
    /// </summary>
    public ColumnDefinition[] Columns { get; set; } = [];

    // ── Stacked header rows ──────────────────────────────────────────────────

    /// <summary>
    /// Ordered list of header row definitions (top-most first).
    /// Each entry corresponds to one header stripe drawn at the top of every page.
    /// ColumnDefinition.Headers[i] supplies the text for HeaderRows[i].
    /// When null or empty a single default header row is used automatically.
    /// </summary>
    public HeaderRowDef[]? HeaderRows { get; set; }

    // ── Footer ───────────────────────────────────────────────────────────────

    /// <summary>Footer configuration. Null = no footer.</summary>
    public FooterDef? Footer { get; set; }

    // ── Table styling ────────────────────────────────────────────────────────

    public TableStyle Style { get; set; } = new();

    /// <summary>Draw a vertical line between every pair of adjacent columns.</summary>
    public bool ShowColumnDividers { get; set; } = false;

    // ── Page layout ──────────────────────────────────────────────────────────

    public PdfPageSize PageSize { get; set; } = PdfPageSize.A4;
    public PdfMargins Margins { get; set; } = PdfMargins.Default;

    /// <summary>Repeat all header rows at the top of every page.</summary>
    public bool RepeatHeadersOnEveryPage { get; set; } = true;

    // Backward-compat aliases
    public bool RepeatHeaderOnEveryPage
    {
        get => RepeatHeadersOnEveryPage;
        set => RepeatHeadersOnEveryPage = value;
    }
    public bool RepeatColumnHeadersOnEveryPage
    {
        get => RepeatHeadersOnEveryPage;
        set => RepeatHeadersOnEveryPage = value;
    }

    // ── Data font ────────────────────────────────────────────────────────────

    /// <summary>Default font size (in points) for data cells. Default 8.</summary>
    public float DataFontSize { get; set; } = 8f;

    // ── Deprecated compat shims (kept so existing code doesn't break) ─────────

    /// <summary>
    /// Shortcut to set column headers from a plain string array.
    /// Creates one <see cref="ColumnDefinition"/> per header with Trim wrap and equal widths.
    /// Prefer <see cref="Columns"/> for full control.
    /// </summary>
    public string[] ColumnHeaders
    {
        get => Columns.Length == 0
            ? []
            : Array.ConvertAll(Columns, c => c.Headers.Length > 0 ? c.Headers[0] : "");
        set
        {
            Columns = Array.ConvertAll(value, h => new ColumnDefinition { Headers = [h] });
            HeaderRows ??= [new HeaderRowDef { Height = 20f, Bold = true }];
        }
    }

    /// <summary>Shortcut to set per-column widths after ColumnHeaders has been set.</summary>
    public float[]? ColumnWidths
    {
        get => Columns.Length == 0 ? null : Array.ConvertAll(Columns, c => c.Width);
        set
        {
            if (value == null || Columns.Length == 0) return;
            for (int i = 0; i < Math.Min(value.Length, Columns.Length); i++)
                Columns[i].Width = value[i];
        }
    }

    // ── Derived measurements ─────────────────────────────────────────────────

    /// <summary>Combined height of all header rows in points.</summary>
    public float TotalHeaderHeight
    {
        get
        {
            var rows = EffectiveHeaderRows;
            float h = 0f;
            foreach (var r in rows) h += r.Height;
            return h;
        }
    }

    /// <summary>Height reserved for the footer (0 when Footer is null).</summary>
    public float FooterHeight => Footer?.Height ?? 0f;

    /// <summary>
    /// Available vertical space per page for data rows after subtracting
    /// margins, header rows, and footer.
    /// </summary>
    public float ContentHeight =>
        PageSize.Height - Margins.Top - Margins.Bottom - TotalHeaderHeight - FooterHeight;

    /// <summary>Content width between left and right margins.</summary>
    public float ContentWidth => PageSize.Width - Margins.Left - Margins.Right;

    /// <summary>The effective header rows (falls back to one default row when HeaderRows is null/empty).</summary>
    internal HeaderRowDef[] EffectiveHeaderRows
    {
        get
        {
            if (HeaderRows is { Length: > 0 }) return HeaderRows;
            return [new HeaderRowDef { Height = 20f, FontSize = 9f, Bold = true }];
        }
    }

    // ── Column width resolution ──────────────────────────────────────────────

    /// <summary>
    /// Resolves all column widths to absolute PDF points.
    /// • Width &gt; 1  → used as-is (absolute points).
    /// • 0 &lt; Width ≤ 1 → treated as fraction of <paramref name="contentWidth"/>.
    /// • Width == 0  → equal share of remaining width.
    /// </summary>
    public float[] GetColumnWidths(float contentWidth)
    {
        if (Columns.Length == 0) return [contentWidth];

        float totalFixed = 0f;
        int autoCount = 0;
        foreach (var col in Columns)
        {
            float w = col.Width;
            if (w <= 0f)          autoCount++;
            else if (w <= 1f)     totalFixed += w * contentWidth;
            else                  totalFixed += w;
        }

        float autoWidth = autoCount > 0
            ? Math.Max(0f, (contentWidth - totalFixed) / autoCount)
            : 0f;

        float[] result = new float[Columns.Length];
        for (int i = 0; i < Columns.Length; i++)
        {
            float w = Columns[i].Width;
            if (w <= 0f)      result[i] = autoWidth;
            else if (w <= 1f) result[i] = w * contentWidth;
            else               result[i] = w;
        }
        return result;
    }
}
