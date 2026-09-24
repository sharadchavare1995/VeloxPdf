using System.IO.Compression;
using VeloxPdf.Diagnostics;

namespace VeloxPdf.Images;

/// <summary>
/// Pure-.NET PNG decoder — no external dependencies.
/// Supports color types 0 (gray), 2 (RGB), 3 (indexed), 4 (gray+alpha), 6 (RGBA).
/// </summary>
public sealed class PngDecoder
{
    public int    Width     { get; private set; }
    public int    Height    { get; private set; }
    public int    BitDepth  { get; private set; }
    public int    ColorType { get; private set; }
    public byte[]? Palette  { get; private set; }

    private static readonly byte[] PngSignature = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

    /// <summary>
    /// Decodes a PNG byte array to raw pixel data.
    /// For RGBA (type 6): returns interleaved RGBA bytes (4 per pixel).
    /// For RGB (type 2): returns RGB bytes (3 per pixel).
    /// For gray+alpha (type 4): returns GA bytes (2 per pixel).
    /// For gray (type 0): returns G bytes (1 per pixel).
    /// For indexed (type 3): returns expanded RGB bytes (3 per pixel).
    /// </summary>
    public byte[] Decode(byte[] pngBytes)
    {
        if (pngBytes == null || pngBytes.Length < 8)
            throw new PdfException(PdfErrorCode.InvalidImage, "PNG data too short.");

        for (int s = 0; s < 8; s++)
        {
            if (pngBytes[s] != PngSignature[s])
                throw new PdfException(PdfErrorCode.InvalidImage, "Invalid PNG signature.");
        }

        var idatChunks = new List<byte[]>();
        int pos = 8;

        while (pos + 8 <= pngBytes.Length)
        {
            int chunkLength = ReadInt32BE(pngBytes, pos); pos += 4;
            if (pos + 4 > pngBytes.Length) break;

            string chunkType = System.Text.Encoding.ASCII.GetString(pngBytes, pos, 4); pos += 4;

            if (pos + chunkLength > pngBytes.Length)
                throw new PdfException(PdfErrorCode.InvalidImage, $"Truncated PNG chunk: {chunkType}");

            switch (chunkType)
            {
                case "IHDR":
                    if (chunkLength < 13)
                        throw new PdfException(PdfErrorCode.InvalidImage, "Invalid IHDR chunk.");
                    Width     = ReadInt32BE(pngBytes, pos);
                    Height    = ReadInt32BE(pngBytes, pos + 4);
                    BitDepth  = pngBytes[pos + 8];
                    ColorType = pngBytes[pos + 9];
                    // index 10=compression, 11=filter method, 12=interlace method
                    if (pngBytes[pos + 12] != 0)
                        throw new PdfException(PdfErrorCode.InvalidImage, "Interlaced PNGs are not supported.");
                    break;

                case "PLTE":
                    if (chunkLength % 3 != 0)
                        throw new PdfException(PdfErrorCode.InvalidImage, "PLTE chunk length must be divisible by 3.");
                    Palette = new byte[chunkLength];
                    Array.Copy(pngBytes, pos, Palette, 0, chunkLength);
                    break;

                case "IDAT":
                    var chunk = new byte[chunkLength];
                    Array.Copy(pngBytes, pos, chunk, 0, chunkLength);
                    idatChunks.Add(chunk);
                    break;

                case "IEND":
                    goto done;
            }

            pos += chunkLength + 4; // skip data + CRC
        }
        done:

        if (Width <= 0 || Height <= 0)
            throw new PdfException(PdfErrorCode.InvalidImage, "PNG IHDR not found or invalid dimensions.");

        if (idatChunks.Count == 0)
            throw new PdfException(PdfErrorCode.InvalidImage, "No IDAT chunks found in PNG.");

        // Concatenate all IDAT chunks
        byte[] compressed = ConcatenateArrays(idatChunks);

        // ZLib decompress
        byte[] filtered = DecompressZlib(compressed);

        // Reconstruct from PNG filters
        return ReconstructFilters(filtered);
    }

    // ── Filter reconstruction ──────────────────────────────────────────────

    private byte[] ReconstructFilters(byte[] filtered)
    {
        int samplesPerPixel = GetInputSamplesPerPixel();
        int bytesPerPixel   = Math.Max(1, (samplesPerPixel * BitDepth + 7) / 8);
        int stride          = (Width * samplesPerPixel * BitDepth + 7) / 8;

        byte[] output  = new byte[Height * stride];
        byte[] prevRow = new byte[stride];
        byte[] currRow = new byte[stride];

        int filterOffset = 0;

        for (int y = 0; y < Height; y++)
        {
            if (filterOffset >= filtered.Length)
                throw new PdfException(PdfErrorCode.InvalidImage, "Unexpected end of PNG filter data.");

            byte filterType = filtered[filterOffset++];

            if (filterOffset + stride > filtered.Length)
                throw new PdfException(PdfErrorCode.InvalidImage, "Truncated PNG scan line.");

            Array.Copy(filtered, filterOffset, currRow, 0, stride);
            filterOffset += stride;

            ApplyFilter(filterType, currRow, prevRow, stride, bytesPerPixel);

            Array.Copy(currRow, 0, output, y * stride, stride);

            // Swap buffers
            byte[] tmp = prevRow;
            prevRow = currRow;
            currRow = tmp;
        }

        // Expand indexed color to RGB
        if (ColorType == 3 && Palette != null)
            return ExpandIndexed(output);

        return output;
    }

    private static void ApplyFilter(byte filterType, byte[] row, byte[] prev, int stride, int bpp)
    {
        switch (filterType)
        {
            case 0: // None
                break;

            case 1: // Sub
                for (int i = bpp; i < stride; i++)
                    row[i] = (byte)(row[i] + row[i - bpp]);
                break;

            case 2: // Up
                for (int i = 0; i < stride; i++)
                    row[i] = (byte)(row[i] + prev[i]);
                break;

            case 3: // Average
                for (int i = 0; i < stride; i++)
                {
                    int left = i >= bpp ? row[i - bpp] : 0;
                    row[i] = (byte)(row[i] + ((left + prev[i]) >> 1));
                }
                break;

            case 4: // Paeth
                for (int i = 0; i < stride; i++)
                {
                    int left   = i >= bpp ? row[i - bpp]  : 0;
                    int upLeft = i >= bpp ? prev[i - bpp] : 0;
                    row[i] = (byte)(row[i] + PaethPredictor((byte)left, prev[i], (byte)upLeft));
                }
                break;

            default:
                throw new PdfException(PdfErrorCode.InvalidImage, $"Unknown PNG filter type: {filterType}");
        }
    }

    private static byte PaethPredictor(byte a, byte b, byte c)
    {
        int p  = a + b - c;
        int pa = Math.Abs(p - a);
        int pb = Math.Abs(p - b);
        int pc = Math.Abs(p - c);
        return pa <= pb && pa <= pc ? a : pb <= pc ? b : c;
    }

    private byte[] ExpandIndexed(byte[] indexed)
    {
        byte[] result = new byte[Width * Height * 3];
        for (int i = 0; i < indexed.Length; i++)
        {
            int palIdx = indexed[i] * 3;
            result[i * 3]     = palIdx     < Palette!.Length ? Palette[palIdx]     : (byte)0;
            result[i * 3 + 1] = palIdx + 1 < Palette.Length  ? Palette[palIdx + 1] : (byte)0;
            result[i * 3 + 2] = palIdx + 2 < Palette.Length  ? Palette[palIdx + 2] : (byte)0;
        }
        return result;
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    /// <summary>Samples per pixel as stored in the PNG file (before expansion).</summary>
    private int GetInputSamplesPerPixel() => ColorType switch
    {
        0 => 1,  // grayscale
        2 => 3,  // RGB
        3 => 1,  // indexed (1 byte per pixel, palette lookup)
        4 => 2,  // grayscale + alpha
        6 => 4,  // RGBA
        _ => throw new PdfException(PdfErrorCode.InvalidImage, $"Unsupported PNG color type: {ColorType}")
    };

    /// <summary>Samples per pixel after expansion (for output to PDF).</summary>
    public int GetOutputSamplesPerPixel() => ColorType switch
    {
        0 => 1,
        2 => 3,
        3 => 3,  // indexed expanded to RGB
        4 => 2,
        6 => 4,
        _ => 3
    };

    private static int ReadInt32BE(byte[] buf, int offset) =>
        (buf[offset] << 24) | (buf[offset + 1] << 16) | (buf[offset + 2] << 8) | buf[offset + 3];

    private static byte[] ConcatenateArrays(List<byte[]> arrays)
    {
        int total = 0;
        for (int i = 0; i < arrays.Count; i++) total += arrays[i].Length;

        byte[] result = new byte[total];
        int pos = 0;
        for (int i = 0; i < arrays.Count; i++)
        {
            arrays[i].CopyTo(result, pos);
            pos += arrays[i].Length;
        }
        return result;
    }

    private static byte[] DecompressZlib(byte[] data)
    {
        using var ms     = new MemoryStream(data);
        using var zlib   = new ZLibStream(ms, CompressionMode.Decompress);
        using var output = new MemoryStream();
        zlib.CopyTo(output);
        return output.ToArray();
    }
}
