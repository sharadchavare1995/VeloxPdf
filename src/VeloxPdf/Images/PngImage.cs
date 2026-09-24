using System.IO.Compression;
using VeloxPdf.Compression;
using VeloxPdf.Primitives;

namespace VeloxPdf.Images;

/// <summary>
/// A PNG image decoded and embedded in a PDF as a FlateDecode image stream.
/// Supports all standard PNG color types including RGBA (alpha → SMask).
/// </summary>
public sealed class PngImage : PdfImage
{
    private readonly PngDecoder _decoder;
    private readonly byte[] _rawPixels;   // decoded pixels (may include alpha)

    public override int PixelWidth  => _decoder.Width;
    public override int PixelHeight => _decoder.Height;

    /// <summary>True if the PNG has an alpha channel (color types 4 or 6).</summary>
    public bool HasAlpha => _decoder.ColorType == 4 || _decoder.ColorType == 6;

    public PngImage(byte[] pngBytes)
    {
        _decoder   = new PngDecoder();
        _rawPixels = _decoder.Decode(pngBytes);
    }

    public PngImage(Stream stream)
    {
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        _decoder   = new PngDecoder();
        _rawPixels = _decoder.Decode(ms.ToArray());
    }

    public override PdfDictionary BuildImageDictionary()
    {
        string colorSpace = _decoder.ColorType switch
        {
            0 => "DeviceGray",   // grayscale
            2 => "DeviceRGB",    // RGB
            3 => "DeviceRGB",    // indexed → expanded to RGB
            4 => "DeviceGray",   // gray + alpha
            6 => "DeviceRGB",    // RGBA
            _ => "DeviceRGB"
        };

        byte[] colorData = ExtractColorData();

        var dict = new PdfDictionary();
        dict.Set("Type",             new PdfName("XObject"))
            .Set("Subtype",          new PdfName("Image"))
            .Set("Width",            new PdfInteger(PixelWidth))
            .Set("Height",           new PdfInteger(PixelHeight))
            .Set("ColorSpace",       new PdfName(colorSpace))
            .Set("BitsPerComponent", new PdfInteger(8))
            .Set("Filter",           new PdfName("FlateDecode"))
            .Set("Length",           new PdfInteger(CompressData(colorData).Length));
        return dict;
    }

    public override byte[] GetEncodedData()
    {
        byte[] colorData = ExtractColorData();
        return CompressData(colorData);
    }

    // ── Alpha / SMask support ──────────────────────────────────────────────

    /// <summary>Extracts the alpha channel as a separate byte array (if present).</summary>
    public byte[]? GetAlphaData()
    {
        if (!HasAlpha) return null;

        int samplesPerPixel = _decoder.ColorType == 4 ? 2 : 4;
        int alphaIndex      = samplesPerPixel - 1;
        int pixelCount      = PixelWidth * PixelHeight;
        byte[] alpha        = new byte[pixelCount];

        for (int i = 0; i < pixelCount; i++)
            alpha[i] = _rawPixels[i * samplesPerPixel + alphaIndex];

        return alpha;
    }

    /// <summary>Builds the /SMask soft-mask dictionary for the alpha channel.</summary>
    public PdfDictionary? BuildSoftMaskDictionary()
    {
        if (!HasAlpha) return null;

        byte[] alphaData      = GetAlphaData()!;
        byte[] compressedAlpha = CompressData(alphaData);

        var dict = new PdfDictionary();
        dict.Set("Type",             new PdfName("XObject"))
            .Set("Subtype",          new PdfName("Image"))
            .Set("Width",            new PdfInteger(PixelWidth))
            .Set("Height",           new PdfInteger(PixelHeight))
            .Set("ColorSpace",       new PdfName("DeviceGray"))
            .Set("BitsPerComponent", new PdfInteger(8))
            .Set("Filter",           new PdfName("FlateDecode"))
            .Set("Length",           new PdfInteger(compressedAlpha.Length));
        return dict;
    }

    /// <summary>Returns the compressed alpha channel bytes (for the SMask stream).</summary>
    public byte[]? GetCompressedAlphaData()
    {
        byte[]? alpha = GetAlphaData();
        return alpha == null ? null : CompressData(alpha);
    }

    // ── Private helpers ────────────────────────────────────────────────────

    private byte[] ExtractColorData()
    {
        if (!HasAlpha)
            return _rawPixels;   // already color-only

        // Strip alpha channel interleaved in raw pixels
        int samplesPerPixel = _decoder.ColorType == 4 ? 2 : 4;  // GA or RGBA
        int colorSamples    = samplesPerPixel - 1;               // G or RGB
        int pixelCount      = PixelWidth * PixelHeight;
        byte[] colorOnly    = new byte[pixelCount * colorSamples];

        for (int i = 0; i < pixelCount; i++)
        {
            int srcBase = i * samplesPerPixel;
            int dstBase = i * colorSamples;
            for (int c = 0; c < colorSamples; c++)
                colorOnly[dstBase + c] = _rawPixels[srcBase + c];
        }

        return colorOnly;
    }

    private static byte[] CompressData(byte[] data)
    {
        var compressor = new FlateCompressor();
        return compressor.Compress(data);
    }
}
