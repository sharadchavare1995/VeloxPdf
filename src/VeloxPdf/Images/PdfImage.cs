using VeloxPdf.Primitives;

namespace VeloxPdf.Images;

/// <summary>Abstract base class for PDF image XObjects.</summary>
public abstract class PdfImage
{
    /// <summary>Resource alias used in page content streams (e.g., "Im1").</summary>
    public string Alias { get; set; } = string.Empty;

    public abstract int PixelWidth  { get; }
    public abstract int PixelHeight { get; }

    /// <summary>Builds the image XObject dictionary.</summary>
    public abstract PdfDictionary BuildImageDictionary();

    /// <summary>Returns the encoded (possibly compressed) image data bytes.</summary>
    public abstract byte[] GetEncodedData();
}
