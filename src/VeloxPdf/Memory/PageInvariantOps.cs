namespace VeloxPdf.Memory;

/// <summary>
/// Caches pre-encoded PDF operator byte sequences that are constant for the lifetime
/// of a page schema â€” computed once, reused for every page to avoid per-page allocations.
/// </summary>
public sealed class PageInvariantOps
{
    private static readonly System.Text.Encoding Latin1 =
        System.Text.Encoding.Latin1;
    private static readonly System.Globalization.CultureInfo Inv =
        System.Globalization.CultureInfo.InvariantCulture;

    // Universal PDF operator bytes
    public byte[] BtBytes          { get; } = Latin1.GetBytes("BT\n");
    public byte[] EtBytes          { get; } = Latin1.GetBytes("ET\n");
    public byte[] SaveStateBytes   { get; } = Latin1.GetBytes("q\n");
    public byte[] RestoreStateBytes{ get; } = Latin1.GetBytes("Q\n");
    public byte[] StrokeBytes      { get; } = Latin1.GetBytes("S\n");
    public byte[] FillBytes        { get; } = Latin1.GetBytes("f\n");
    public byte[] BlackFillBytes   { get; } = Latin1.GetBytes("0 0 0 rg\n");
    public byte[] WhiteFillBytes   { get; } = Latin1.GetBytes("1 1 1 rg\n");

    // Font set operators â€” pre-encoded for data and header fonts
    public byte[] DataFontSetBytes   { get; }
    public byte[] HeaderFontSetBytes { get; }

    // Pre-encoded column X-position strings (one entry per column)
    public byte[][] ColumnXBytes { get; }

    public PageInvariantOps(
        string dataFontAlias,   float dataFontSize,
        string headerFontAlias, float headerFontSize,
        float[] columnXPositions)
    {
        DataFontSetBytes   = Latin1.GetBytes($"/{dataFontAlias} {Fmt(dataFontSize)} Tf\n");
        HeaderFontSetBytes = Latin1.GetBytes($"/{headerFontAlias} {Fmt(headerFontSize)} Tf\n");

        ColumnXBytes = new byte[columnXPositions.Length][];
        for (int i = 0; i < columnXPositions.Length; i++)
            ColumnXBytes[i] = Latin1.GetBytes(Fmt(columnXPositions[i]));
    }

    private static string Fmt(float v) => v.ToString("0.####", Inv);
}

