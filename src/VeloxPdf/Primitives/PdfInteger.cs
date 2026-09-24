namespace VeloxPdf.Primitives;

/// <summary>PDF integer object.</summary>
public sealed class PdfInteger : PdfObject
{
    public int Value { get; }

    public PdfInteger(int value) => Value = value;

    public static implicit operator PdfInteger(int value) => new(value);

    public override void WriteTo(IO.PdfWriter writer)
        => writer.WriteRaw(Value.ToString(CultureInfo.InvariantCulture));

    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);
}
