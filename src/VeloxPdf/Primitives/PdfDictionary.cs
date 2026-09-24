namespace VeloxPdf.Primitives;

/// <summary>
/// PDF dictionary object. Maintains insertion order.
/// Keys are stored without leading slash; the slash is added during serialization.
/// </summary>
public sealed class PdfDictionary : PdfObject
{
    private readonly List<KeyValuePair<string, PdfObject>> _entries = new();

    public int Count => _entries.Count;

    public IEnumerable<KeyValuePair<string, PdfObject>> Entries => _entries;

    // ── Setters ─────────────────────────────────────────────────────────────

    public PdfDictionary Set(string key, PdfObject value)
    {
        // Update existing or append
        for (int i = 0; i < _entries.Count; i++)
        {
            if (_entries[i].Key == key)
            {
                _entries[i] = new KeyValuePair<string, PdfObject>(key, value);
                return this;
            }
        }
        _entries.Add(new KeyValuePair<string, PdfObject>(key, value));
        return this;
    }

    public PdfDictionary Set(string key, int value)          => Set(key, new PdfInteger(value));
    public PdfDictionary Set(string key, float value)        => Set(key, new PdfReal(value));
    public PdfDictionary Set(string key, bool value)         => Set(key, new PdfBoolean(value));

    /// <summary>Sets key to a PdfName value.</summary>
    public PdfDictionary Set(string key, string value, bool isName = true)
        => isName ? Set(key, new PdfName(value)) : Set(key, new PdfString(value));

    public PdfDictionary Remove(string key)
    {
        _entries.RemoveAll(e => e.Key == key);
        return this;
    }

    // ── Getters ─────────────────────────────────────────────────────────────

    public PdfObject? Get(string key)
    {
        foreach (var e in _entries)
            if (e.Key == key) return e.Value;
        return null;
    }

    public bool TryGet<T>(string key, out T? value) where T : PdfObject
    {
        var obj = Get(key);
        if (obj is T typed)
        {
            value = typed;
            return true;
        }
        value = null;
        return false;
    }

    public bool ContainsKey(string key)
    {
        foreach (var e in _entries)
            if (e.Key == key) return true;
        return false;
    }

    // ── Serialization ────────────────────────────────────────────────────────

    public override void WriteTo(IO.PdfWriter writer)
    {
        writer.WriteRaw("<<");
        foreach (var e in _entries)
        {
            writer.WriteRaw("\n/");
            writer.WriteRaw(e.Key);
            writer.WriteRaw(" ");
            e.Value.WriteTo(writer);
        }
        writer.WriteRaw("\n>>");
    }

    public override string ToString()
    {
        var sb = new StringBuilder("<<");
        foreach (var e in _entries)
        {
            sb.Append("\n/").Append(e.Key).Append(' ').Append(e.Value);
        }
        sb.Append("\n>>");
        return sb.ToString();
    }
}
