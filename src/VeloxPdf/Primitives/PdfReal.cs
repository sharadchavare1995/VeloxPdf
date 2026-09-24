namespace VeloxPdf.Primitives;

/// <summary>PDF real (floating-point) object. Formats with max 4 decimal places, no trailing zeros.</summary>
public sealed class PdfReal : PdfObject
{
    public float Value { get; }

    public PdfReal(float value) => Value = value;

    public static implicit operator PdfReal(float value)  => new(value);
    public static implicit operator PdfReal(double value) => new((float)value);

    private static string Format(float f)
    {
        // Use G4 gives up to 4 significant digits; we want max 4 decimal places
        string s = f.ToString("0.####", CultureInfo.InvariantCulture);
        return s;
    }

    public override void WriteTo(IO.PdfWriter writer) => writer.WriteRaw(Format(Value));

    public override string ToString() => Format(Value);
}
