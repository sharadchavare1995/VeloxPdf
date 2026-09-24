namespace VeloxPdf.DependencyInjection;
using System.IO.Compression;
using VeloxPdf.Document;

public sealed class VeloxPdfOptions
{
    public bool EnableCompression { get; set; } = true;
    public CompressionLevel CompressionLevel { get; set; } = CompressionLevel.Optimal;
    public bool EmbedFonts { get; set; } = true;
    public bool SubsetFonts { get; set; } = true;
    public int MaxConcurrentGenerations { get; set; } = Environment.ProcessorCount * 2;
    public string DefaultFontFamily { get; set; } = "Helvetica";
    public float DefaultFontSize { get; set; } = 11f;
    public PdfPageSize DefaultPageSize { get; set; } = PdfPageSize.A4;
    public PdfMargins DefaultMargins { get; set; } = PdfMargins.Default;
}
