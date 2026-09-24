namespace VeloxPdf.Templates;
using System.Collections.Concurrent;

public interface ITemplateRegistry
{
    void Register(ITemplate template);
    ITemplate Get(string name);
    bool TryGet(string name, out ITemplate? template);
    IReadOnlyList<string> AvailableTemplates { get; }
}

public sealed class TemplateRegistry : ITemplateRegistry
{
    private readonly ConcurrentDictionary<string, ITemplate> _templates = new(
        StringComparer.OrdinalIgnoreCase);

    public void Register(ITemplate template)
    {
        _templates[template.Name] = template;
    }

    public ITemplate Get(string name)
    {
        if (_templates.TryGetValue(name, out ITemplate? template))
            return template;
        throw new Diagnostics.PdfException(
            Diagnostics.PdfErrorCode.TemplateNotFound,
            $"Template '{name}' not found. Available: {string.Join(", ", _templates.Keys)}");
    }

    public bool TryGet(string name, out ITemplate? template)
        => _templates.TryGetValue(name, out template);

    public IReadOnlyList<string> AvailableTemplates =>
        _templates.Keys.OrderBy(k => k).ToList();

    public int Count => _templates.Count;
}
