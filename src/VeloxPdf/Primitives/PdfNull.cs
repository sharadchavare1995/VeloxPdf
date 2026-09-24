namespace VeloxPdf.Primitives;

/// <summary>PDF null object.</summary>
public sealed class PdfNull : PdfObject
{
    /// <summary>Singleton instance.</summary>
    public static readonly PdfNull Instance = new();

    private PdfNull() { }

    public override void WriteTo(IO.PdfWriter writer) => writer.WriteRaw("null");
}
