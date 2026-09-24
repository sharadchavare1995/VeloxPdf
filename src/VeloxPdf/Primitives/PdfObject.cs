namespace VeloxPdf.Primitives;

/// <summary>Abstract base class for all PDF objects.</summary>
public abstract class PdfObject
{
    /// <summary>PDF object number (0 = not yet assigned / inline).</summary>
    public int ObjectNumber { get; set; }

    /// <summary>PDF generation number (almost always 0 in new PDFs).</summary>
    public int Generation { get; internal set; } = 0;

    /// <summary>Indirect reference string, e.g. "5 0 R".</summary>
    public string Reference => $"{ObjectNumber} {Generation} R";

    /// <summary>True when this object has been assigned an object number.</summary>
    public bool IsIndirect => ObjectNumber > 0;

    /// <summary>Serialize this object to the PDF output stream.</summary>
    public abstract void WriteTo(IO.PdfWriter writer);
}
