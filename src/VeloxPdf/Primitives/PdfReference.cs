namespace VeloxPdf.Primitives;

/// <summary>PDF indirect reference (N G R).</summary>
public sealed class PdfReference : PdfObject
{
    public int ReferencedObjectNumber { get; }
    public int ReferencedGeneration   { get; }

    public PdfReference(int objNum, int gen = 0)
    {
        ReferencedObjectNumber = objNum;
        ReferencedGeneration   = gen;
    }

    public PdfReference(PdfObject obj)
    {
        ReferencedObjectNumber = obj.ObjectNumber;
        ReferencedGeneration   = obj.Generation;
    }

    public override void WriteTo(IO.PdfWriter writer)
        => writer.WriteRaw($"{ReferencedObjectNumber} {ReferencedGeneration} R");

    public override string ToString()
        => $"{ReferencedObjectNumber} {ReferencedGeneration} R";
}
