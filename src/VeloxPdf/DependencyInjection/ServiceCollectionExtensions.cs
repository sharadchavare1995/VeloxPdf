namespace VeloxPdf.DependencyInjection;
using VeloxPdf.Compression;
using VeloxPdf.Templates;

/// <summary>
/// Extension methods for registering VeloxPdf services.
/// These are self-contained and do not depend on any external DI framework.
/// For ASP.NET Core / Microsoft.Extensions.DependencyInjection integration,
/// see the VeloxPdfServiceCollection static class below.
/// </summary>
public static class VeloxPdfServiceCollection
{
    /// <summary>
    /// Builds a fully-configured VeloxPdf service container with all default templates registered.
    /// </summary>
    public static VeloxPdfContainer Build(Action<VeloxPdfOptions>? configure = null)
    {
        var options = new VeloxPdfOptions();
        configure?.Invoke(options);
        return new VeloxPdfContainer(options);
    }
}

/// <summary>
/// A lightweight, self-contained service container for VeloxPdf.
/// Provides access to the TemplateEngine and TemplateRegistry without any external DI framework.
/// </summary>
public sealed class VeloxPdfContainer
{
    public ITemplateRegistry Registry { get; }
    public TemplateEngine Engine { get; }
    public VeloxPdfOptions Options { get; }
    public ICompressor Compressor { get; }

    public VeloxPdfContainer(VeloxPdfOptions options)
    {
        Options = options;
        Compressor = options.EnableCompression
            ? new FlateCompressor(options.CompressionLevel)
            : (ICompressor)new NoCompressor();

        var registry = new TemplateRegistry();
        // Register default templates
        registry.Register(new InvoiceTemplate());
        registry.Register(new ReportTemplate());
        registry.Register(new LetterTemplate());

        Registry = registry;
        Engine = new TemplateEngine(Registry);
    }

    /// <summary>Register a custom template into this container's registry.</summary>
    public VeloxPdfContainer AddTemplate(ITemplate template)
    {
        Registry.Register(template);
        return this;
    }

    /// <summary>Register a custom template by type (must have parameterless constructor).</summary>
    public VeloxPdfContainer AddTemplate<T>() where T : ITemplate, new()
    {
        Registry.Register(new T());
        return this;
    }

    /// <summary>Generate a PDF from a named template.</summary>
    public byte[] Generate(string templateName, TemplateData data)
        => Engine.Generate(templateName, data);

    /// <summary>Generate a PDF from a named template asynchronously.</summary>
    public Task<byte[]> GenerateAsync(string templateName, TemplateData data,
        CancellationToken ct = default)
        => Engine.GenerateAsync(templateName, data, ct);
}
