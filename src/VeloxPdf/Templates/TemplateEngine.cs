namespace VeloxPdf.Templates;
using System.Diagnostics;
using VeloxPdf.Diagnostics;

public sealed class TemplateEngine
{
    private readonly ITemplateRegistry _registry;
    public event Action<PdfGenerationMetrics>? OnMetrics;

    public TemplateEngine(ITemplateRegistry registry)
    {
        _registry = registry;
    }

    public byte[] Generate(string templateName, TemplateData data)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var template = _registry.Get(templateName);
            var doc = template.Build(data);
            byte[] bytes = doc.Save();
            sw.Stop();
            OnMetrics?.Invoke(PdfGenerationMetrics.ForSuccess(
                templateName, doc.Pages.Count, bytes.Length, sw.Elapsed));
            return bytes;
        }
        catch (Exception ex)
        {
            sw.Stop();
            OnMetrics?.Invoke(PdfGenerationMetrics.ForFailure(
                templateName, sw.Elapsed, ex.Message));
            throw;
        }
    }

    public async Task<byte[]> GenerateAsync(string templateName, TemplateData data,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return await Task.Run(() => Generate(templateName, data), ct);
    }

    public async Task GenerateToStreamAsync(string templateName, TemplateData data,
        Stream output, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        byte[] bytes = await GenerateAsync(templateName, data, ct);
        await output.WriteAsync(bytes, ct);
    }
}
