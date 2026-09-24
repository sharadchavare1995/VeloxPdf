namespace VeloxPdf.Primitives;

/// <summary>PDF array object. Ordered collection of PDF objects.</summary>
public sealed class PdfArray : PdfObject
{
    private readonly List<PdfObject> _items = new();

    public int Count => _items.Count;

    public PdfObject this[int index]
    {
        get => _items[index];
        set => _items[index] = value;
    }

    public void Add(PdfObject obj) => _items.Add(obj);

    public void Add(int value) => _items.Add(new PdfInteger(value));

    public void Add(float value) => _items.Add(new PdfReal(value));

    /// <summary>Adds a PDF name (the string should be the name without the slash).</summary>
    public void Add(string name) => _items.Add(new PdfName(name));

    public IEnumerable<PdfObject> GetItems() => _items;

    public override void WriteTo(IO.PdfWriter writer)
    {
        writer.WriteRaw("[ ");
        for (int i = 0; i < _items.Count; i++)
        {
            _items[i].WriteTo(writer);
            writer.WriteRaw(" ");
        }
        writer.WriteRaw("]");
    }

    public override string ToString()
    {
        var sb = new StringBuilder("[ ");
        foreach (var item in _items)
        {
            sb.Append(item.ToString());
            sb.Append(' ');
        }
        sb.Append(']');
        return sb.ToString();
    }
}
