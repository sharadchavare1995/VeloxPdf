namespace VeloxPdf.Streaming;

/// <summary>How a cell's content is handled when it exceeds the column's available width.</summary>
public enum CellWrapMode
{
    /// <summary>Text wraps to the next line. The row height expands to fit all lines.</summary>
    Wrap,
    /// <summary>Text is truncated and a "..." suffix appended if it exceeds the column width.
    /// The row height stays at one line. Text never overlaps the next column.</summary>
    Trim,
    /// <summary>Text is hard-clipped at the column edge using a PDF clipping path.
    /// No ellipsis is added. Row height stays at one line.</summary>
    Clip
}

/// <summary>Horizontal alignment of text within a cell.</summary>
public enum CellAlign { Left, Center, Right }

/// <summary>
/// Complete definition for one table column: width, wrapping behaviour, alignment,
/// and per-level header texts (for stacked multi-row headers).
/// </summary>
public sealed class ColumnDefinition
{
    /// <summary>
    /// Column width in points.
    /// • &gt; 1  → absolute width in PDF points.
    /// • 0–1 inclusive → fraction of the total content width (e.g. 0.20 = 20 %).
    /// • 0   → auto: share the remaining width equally with other auto columns.
    /// </summary>
    public float Width { get; set; } = 0f;

    /// <summary>How cell text is handled when it does not fit in one line.</summary>
    public CellWrapMode Wrap { get; set; } = CellWrapMode.Trim;

    /// <summary>Horizontal alignment of the data cell text.</summary>
    public CellAlign Align { get; set; } = CellAlign.Left;

    /// <summary>
    /// Header label for each stacked header level.
    /// Index 0 = top-most header row, index 1 = second row, etc.
    /// When the schema has more HeaderRowDef entries than this array has elements,
    /// the remaining levels will show an empty cell for this column.
    /// </summary>
    public string[] Headers { get; set; } = [];

    /// <summary>Override data font size (in points) for this column only. Null = schema default.</summary>
    public float? FontSize { get; set; }

    // ── Convenience factories ────────────────────────────────────────────────

    /// <summary>Create a trimming (non-wrapping) column with one header label.</summary>
    public static ColumnDefinition Create(
        string header,
        float width = 0f,
        CellWrapMode wrap = CellWrapMode.Trim,
        CellAlign align = CellAlign.Left)
        => new() { Headers = [header], Width = width, Wrap = wrap, Align = align };

    /// <summary>Create a column whose text wraps to multiple lines when it overflows.</summary>
    public static ColumnDefinition Wrapping(string header, float width = 0f, CellAlign align = CellAlign.Left)
        => new() { Headers = [header], Width = width, Wrap = CellWrapMode.Wrap, Align = align };

    /// <summary>Create a right-aligned numeric column.</summary>
    public static ColumnDefinition Numeric(string header, float width = 0f)
        => new() { Headers = [header], Width = width, Wrap = CellWrapMode.Trim, Align = CellAlign.Right };

    /// <summary>Create a centered column.</summary>
    public static ColumnDefinition Centered(string header, float width = 0f)
        => new() { Headers = [header], Width = width, Wrap = CellWrapMode.Trim, Align = CellAlign.Center };
}
