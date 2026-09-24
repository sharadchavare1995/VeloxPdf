namespace VeloxPdf.Streaming;
using VeloxPdf.Color;

/// <summary>
/// Configures the fixed-height footer that appears at the bottom of every page.
/// </summary>
public sealed class FooterDef
{
    /// <summary>Height of the footer area in PDF points. Default 24.</summary>
    public float Height { get; set; } = 24f;

    // ── Page number ──────────────────────────────────────────────────────────

    /// <summary>Show the page number on the right side of the footer.</summary>
    public bool ShowPageNumber { get; set; } = true;

    /// <summary>
    /// Format of the page number text.
    /// {n} is replaced with the current page number.
    /// Example: "Page {n}"  →  "Page 42"
    /// </summary>
    public string PageNumberFormat { get; set; } = "Page {n}";

    // ── Print date ───────────────────────────────────────────────────────────

    /// <summary>Show the print date/time on the left side of the footer.</summary>
    public bool ShowPrintDate { get; set; } = true;

    /// <summary>
    /// Format of the print-date text.
    /// {d} is replaced with the formatted date.
    /// Example: "Printed: {d}"  →  "Printed: 2026-09-11 14:30"
    /// </summary>
    public string PrintDateFormat { get; set; } = "Printed: {d}";

    /// <summary>
    /// DateTime format passed to DateTime.Now.ToString().
    /// Default is ISO-8601 date+time: "yyyy-MM-dd HH:mm".
    /// </summary>
    public string DateFormat { get; set; } = "yyyy-MM-dd HH:mm";

    // ── Custom text ──────────────────────────────────────────────────────────

    /// <summary>
    /// Custom text for the left-hand side. When set, it replaces the print date there.
    /// Supports {n} (page number) and {d} (formatted date).
    /// </summary>
    public string? LeftText { get; set; }

    /// <summary>
    /// Custom text centred in the footer.
    /// Supports {n} (page number) and {d} (formatted date).
    /// </summary>
    public string? CenterText { get; set; }

    /// <summary>
    /// Custom text for the right-hand side. When set, it replaces the page number there.
    /// Supports {n} (page number) and {d} (formatted date).
    /// </summary>
    public string? RightText { get; set; }

    // ── Appearance ───────────────────────────────────────────────────────────

    /// <summary>Background fill colour for the footer strip.
    /// Null = no background (transparent / white).</summary>
    public PdfColor? Background { get; set; }

    /// <summary>Colour of footer text. Null = mid-gray (#6B7280).</summary>
    public PdfColor? TextColor { get; set; }

    /// <summary>Font size for all footer text in points. Default 7.5.</summary>
    public float FontSize { get; set; } = 7.5f;

    // ── Top border ───────────────────────────────────────────────────────────

    /// <summary>Draw a horizontal line separating the table body from the footer.</summary>
    public bool ShowTopBorder { get; set; } = true;

    /// <summary>Width of the top-border line in points. Default 0.5.</summary>
    public float TopBorderWidth { get; set; } = 0.5f;

    /// <summary>Colour of the top-border line. Null = TableStyle.BorderColor.</summary>
    public PdfColor? TopBorderColor { get; set; }

    // ── Convenience factories ────────────────────────────────────────────────

    /// <summary>Default footer: print date left, page number right, thin top border.</summary>
    public static FooterDef Default(float height = 24f) => new() { Height = height };

    /// <summary>Minimal footer: page number only, no print date, no background.</summary>
    public static FooterDef PageNumberOnly(float height = 20f) => new()
    {
        Height        = height,
        ShowPageNumber = true,
        ShowPrintDate  = false
    };

    /// <summary>Full footer: custom centre text, date left, page number right.</summary>
    public static FooterDef WithTitle(string centerTitle, float height = 24f) => new()
    {
        Height     = height,
        CenterText = centerTitle
    };
}
