namespace VeloxPdf.Document;
using VeloxPdf.Fonts;
using VeloxPdf.Graphics;
using VeloxPdf.Images;
using VeloxPdf.Primitives;

public sealed class PdfPage
{
    public float Width { get; }
    public float Height { get; }
    public PdfMargins Margins { get; }
    public ContentStream ContentStream { get; } = new();
    public List<Elements.IPdfElement> Elements { get; } = new();

    private readonly Dictionary<string, PdfFont> _fonts = new();
    private readonly Dictionary<string, PdfImage> _images = new();
    private int _fontCounter = 0;
    private int _imageCounter = 0;

    public PdfPage(float width, float height, PdfMargins margins)
    {
        Width = width;
        Height = height;
        Margins = margins;
    }

    public string RegisterFont(PdfFont font)
    {
        // Check if already registered by BaseFont name
        foreach (var kvp in _fonts)
            if (kvp.Value.BaseFont == font.BaseFont) return kvp.Key;

        string newAlias = $"F{++_fontCounter}";
        font.Alias = newAlias;
        _fonts[newAlias] = font;
        return newAlias;
    }

    public string RegisterImage(PdfImage image)
    {
        foreach (var kvp in _images)
            if (ReferenceEquals(kvp.Value, image)) return kvp.Key;

        string newAlias = $"Im{++_imageCounter}";
        image.Alias = newAlias;
        _images[newAlias] = image;
        return newAlias;
    }

    public IReadOnlyDictionary<string, PdfFont> Fonts => _fonts;
    public IReadOnlyDictionary<string, PdfImage> Images => _images;

    public PdfDictionary BuildResourceDictionary()
    {
        var resources = new PdfDictionary();

        if (_fonts.Count > 0)
        {
            var fontDict = new PdfDictionary();
            foreach (var (alias, font) in _fonts)
            {
                fontDict.Set(alias, font.BuildFontDictionary());
            }
            resources.Set("Font", fontDict);
        }

        if (_images.Count > 0)
        {
            var xobjectDict = new PdfDictionary();
            foreach (var (alias, image) in _images)
            {
                xobjectDict.Set(alias, new PdfDictionary());
            }
            resources.Set("XObject", xobjectDict);
        }

        resources.Set("ProcSet", new PdfArray().Also(a =>
        {
            a.Add("PDF"); a.Add("Text");
            if (_images.Count > 0) { a.Add("ImageB"); a.Add("ImageC"); a.Add("ImageI"); }
        }));

        return resources;
    }
}

// Helper extension for fluent initialization
internal static class PageExtensions
{
    public static T Also<T>(this T obj, Action<T> action) { action(obj); return obj; }
}
