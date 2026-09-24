namespace VeloxPdf.Primitives;

/// <summary>PDF boolean object.</summary>
public sealed class PdfBoolean : PdfObject
{
    public static readonly PdfBoolean True  = new(true);
    public static readonly PdfBoolean False = new(false);

    public bool Value { get; }

    public PdfBoolean(bool value) => Value = value;

    public static implicit operator PdfBoolean(bool value) => value ? True : False;

    public override void WriteTo(IO.PdfWriter writer)
        => writer.WriteRaw(Value ? "true" : "false");

    public override string ToString() => Value ? "true" : "false";
}
