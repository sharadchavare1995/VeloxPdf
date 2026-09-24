namespace VeloxPdf.Streaming;
using VeloxPdf.Color;

/// <summary>
/// Defines one level of the table's column-header area.
/// Add multiple HeaderRowDef entries to StreamingTableSchema.HeaderRows
/// to produce stacked headers (one below another).
/// </summary>
public sealed class HeaderRowDef
{
    /// <summary>Height of this header row in PDF points.</summary>
    public float Height { get; set; } = 20f;

    /// <summary>
    /// Background fill colour for this header row.
    /// Null = use TableStyle.HeaderBackgroundColor (default dark blue #2563EB).
    /// </summary>
    public PdfColor? Background { get; set; }

    /// <summary>
    /// Text colour for cells in this header row.
    /// Null = white.
    /// </summary>
    public PdfColor? TextColor { get; set; }

    /// <summary>Font size for text in this header row (in points).</summary>
    public float FontSize { get; set; } = 9f;

    /// <summary>True = Helvetica-Bold (F2); false = Helvetica (F1).</summary>
    public bool Bold { get; set; } = true;

    /// <summary>Draw a thin horizontal line below this header row.</summary>
    public bool ShowBottomBorder { get; set; } = false;

    /// <summary>Draw a thin horizontal line above this header row (top of the table).</summary>
    public bool ShowTopBorder { get; set; } = false;

    /// <summary>Per-column alignment override for this header level.
    /// Null means all columns use CellAlign.Left.
    /// Array length may be shorter than Columns — remaining columns default to Left.
    /// </summary>
    public CellAlign[]? ColumnAlignments { get; set; }

    // ── Convenience factories ────────────────────────────────────────────────

    /// <summary>Default primary header row (dark blue background, white bold text).</summary>
    public static HeaderRowDef Primary(float height = 20f) => new()
    {
        Height     = height,
        FontSize   = 9f,
        Bold       = true,
        Background = PdfColor.PrimaryBlue
    };

    /// <summary>A secondary/sub-header row (slightly lighter blue, same text).</summary>
    public static HeaderRowDef Secondary(float height = 18f) => new()
    {
        Height     = height,
        FontSize   = 8.5f,
        Bold       = false,
        Background = PdfColor.FromHex("#3B82F6")  // Tailwind blue-500
    };

    /// <summary>A dark sub-header row (used for grouping spans across multiple columns).</summary>
    public static HeaderRowDef Dark(float height = 18f) => new()
    {
        Height     = height,
        FontSize   = 8.5f,
        Bold       = true,
        Background = PdfColor.DarkBlue
    };
}
