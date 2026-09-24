namespace VeloxPdf.Streaming;

/// <summary>Style of a horizontal separator line inserted between data rows.</summary>
public enum LineStyle
{
    /// <summary>One thin line using TableStyle.BorderWidth and BorderColor.</summary>
    Single,
    /// <summary>Two parallel thin lines spaced 2.5 pt apart.</summary>
    Double,
    /// <summary>One thick line (1.5 × normal border width).</summary>
    Thick
}

/// <summary>
/// A row in the streaming table. Either a data row (cells array) or a visual separator line.
/// <para>
/// Create with the static factory methods or implicitly cast a <c>string[]</c>:
/// </para>
/// <code>
/// yield return TableRow.Data(new[] { "1", "Alice", "$50,000" });
/// yield return TableRow.Line();          // single horizontal rule
/// yield return TableRow.Line(LineStyle.Double);
/// yield return new string[] { "2", "Bob", "$60,000" };  // implicit conversion
/// </code>
/// </summary>
public sealed class TableRow
{
    /// <summary>Cell values for a data row. Null when this is a separator.</summary>
    public string[]? Cells { get; private init; }

    /// <summary>True when this row is a drawn horizontal rule rather than data.</summary>
    public bool IsSeparator { get; private init; }

    /// <summary>Style of the separator line (meaningful only when IsSeparator = true).</summary>
    public LineStyle SeparatorStyle { get; private init; } = LineStyle.Single;

    /// <summary>
    /// Fixed height override in PDF points.
    /// For data rows: overrides auto-calculated height (useful with pre-wrapped text).
    /// For separators: total vertical space reserved for the rule (including margins).
    /// Null = auto.
    /// </summary>
    public float? Height { get; private init; }

    // ── Factory methods ──────────────────────────────────────────────────────

    /// <summary>Create a data row from a cell array.</summary>
    public static TableRow Data(string[] cells, float? height = null)
        => new() { Cells = cells, IsSeparator = false, Height = height };

    /// <summary>Insert a single horizontal rule.</summary>
    public static TableRow Line(LineStyle style = LineStyle.Single, float? height = null)
        => new() { IsSeparator = true, SeparatorStyle = style, Height = height };

    /// <summary>Shorthand for a double-line separator.</summary>
    public static TableRow DoubleLine(float? height = null)
        => Line(LineStyle.Double, height);

    /// <summary>Shorthand for a thick-line separator.</summary>
    public static TableRow ThickLine(float? height = null)
        => Line(LineStyle.Thick, height);

    // ── Implicit conversions for backward compatibility ───────────────────────

    /// <summary>Implicitly wrap a string[] as a data TableRow.</summary>
    public static implicit operator TableRow(string[] cells) => Data(cells);
}
