namespace VeloxPdf.Templates;
using VeloxPdf.Document;
using VeloxPdf.Elements;

public sealed class TemplateData
{
    public string Title { get; set; } = "";
    public Dictionary<string, string> Properties { get; set; } = new();
    public List<IPdfElement> CustomSections { get; set; } = new();
    public PdfMetadata Metadata { get; set; } = new();
    public Dictionary<string, object> Data { get; set; } = new();

    public string Get(string key, string defaultValue = "") =>
        Properties.TryGetValue(key, out string? val) ? val : defaultValue;

    public T Get<T>(string key, T defaultValue = default!)
    {
        if (Data.TryGetValue(key, out object? val) && val is T typed)
            return typed;
        if (Properties.TryGetValue(key, out string? strVal))
        {
            try { return (T)Convert.ChangeType(strVal, typeof(T)); }
            catch { }
        }
        return defaultValue;
    }

    public bool HasKey(string key) =>
        Properties.ContainsKey(key) || Data.ContainsKey(key);
}
