using VeloxPdf.Diagnostics;
using VeloxPdf.Primitives;

namespace VeloxPdf.Images;

/// <summary>
/// A JPEG image for embedding in a PDF via /DCTDecode.
/// The raw JPEG bytes are embedded as-is — no re-encoding.
/// </summary>
public sealed class JpegImage : PdfImage
{
    private readonly byte[] _jpegBytes;
    private readonly int _width;
    private readonly int _height;
    private readonly int _components;

    public override int PixelWidth  => _width;
    public override int PixelHeight => _height;

    /// <summary>Number of color components (1 = grayscale, 3 = RGB, 4 = CMYK).</summary>
    public int Components => _components;

    public JpegImage(byte[] jpegBytes)
    {
        _jpegBytes = jpegBytes ?? throw new ArgumentNullException(nameof(jpegBytes));
        (_width, _height, _components) = ParseSofMarker(jpegBytes);
    }

    public JpegImage(Stream stream)
    {
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        _jpegBytes = ms.ToArray();
        (_width, _height, _components) = ParseSofMarker(_jpegBytes);
    }

    public override PdfDictionary BuildImageDictionary()
    {
        string colorSpace = _components switch
        {
            1 => "DeviceGray",
            4 => "DeviceCMYK",
            _ => "DeviceRGB",
        };

        var dict = new PdfDictionary();
        dict.Set("Type",             new PdfName("XObject"))
            .Set("Subtype",          new PdfName("Image"))
            .Set("Width",            new PdfInteger(_width))
            .Set("Height",           new PdfInteger(_height))
            .Set("ColorSpace",       new PdfName(colorSpace))
            .Set("BitsPerComponent", new PdfInteger(8))
            .Set("Filter",           new PdfName("DCTDecode"))
            .Set("Length",           new PdfInteger(_jpegBytes.Length));
        return dict;
    }

    public override byte[] GetEncodedData() => _jpegBytes;

    // ── Private: JPEG SOF marker parser ──────────────────────────────────────

    private static (int width, int height, int components) ParseSofMarker(byte[] data)
    {
        if (data.Length < 4 || data[0] != 0xFF || data[1] != 0xD8)
            throw new PdfException(PdfErrorCode.InvalidImage, "Not a valid JPEG file.");

        int i = 2;
        while (i < data.Length - 1)
        {
            if (data[i] != 0xFF)
            {
                i++;
                continue;
            }

            byte marker = data[i + 1];

            // SOF markers: 0xC0 (baseline), 0xC1, 0xC2 (progressive), 0xC3, 0xC5-0xC7, 0xC9-0xCB, 0xCD-0xCF
            if ((marker >= 0xC0 && marker <= 0xC3) ||
                (marker >= 0xC5 && marker <= 0xC7) ||
                (marker >= 0xC9 && marker <= 0xCB) ||
                (marker >= 0xCD && marker <= 0xCF))
            {
                // SOF: FF Cx [length 2 bytes] [precision 1] [height 2] [width 2] [components 1]
                if (i + 9 >= data.Length)
                    throw new PdfException(PdfErrorCode.InvalidImage, "Truncated JPEG SOF marker.");

                // int segLength = (data[i+2] << 8) | data[i+3]; // not needed
                int height     = (data[i + 5] << 8) | data[i + 6];
                int width      = (data[i + 7] << 8) | data[i + 8];
                int components = data[i + 9];
                return (width, height, components);
            }

            // Skip over this marker's data
            if (i + 3 < data.Length)
            {
                int segLen = (data[i + 2] << 8) | data[i + 3];
                i += 2 + segLen;
            }
            else
            {
                i += 2;
            }
        }

        throw new PdfException(PdfErrorCode.InvalidImage, "JPEG SOF marker not found.");
    }
}
