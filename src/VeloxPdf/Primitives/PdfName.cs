namespace VeloxPdf.Primitives;

/// <summary>
/// PDF name object. Stored without leading slash; written with leading slash.
/// Non-regular characters are escaped as #XX.
/// </summary>
public sealed class PdfName : PdfObject, IEquatable<PdfName>
{
    // Characters that do NOT need escaping in a PDF name (PDF 1.7 spec table 4)
    private static bool IsRegular(char c) =>
        c > 0x20 && c < 0x7F &&
        c != '(' && c != ')' && c != '<' && c != '>' &&
        c != '[' && c != ']' && c != '{' && c != '}' &&
        c != '/' && c != '%' && c != '#';

    private static readonly System.Text.Encoding _latin1 = System.Text.Encoding.Latin1;

    /// <summary>Name without the leading slash.</summary>
    public string Name { get; }

    public PdfName(string name)
    {
        // Strip leading slash if caller accidentally included it
        Name = name.StartsWith('/') ? name[1..] : name;
    }

    private string Escaped()
    {
        var sb = new StringBuilder(Name.Length + 4);
        foreach (char c in Name)
        {
            if (IsRegular(c))
                sb.Append(c);
            else
                sb.Append($"#{(int)c:X2}");
        }
        return sb.ToString();
    }

    public override void WriteTo(IO.PdfWriter writer)
    {
        writer.WriteRaw("/");
        writer.WriteRaw(Escaped());
    }

    public override string ToString() => "/" + Escaped();

    public bool Equals(PdfName? other) => other is not null && Name == other.Name;
    public override bool Equals(object? obj) => obj is PdfName n && Equals(n);
    public override int GetHashCode() => Name.GetHashCode(StringComparison.Ordinal);

    // â”€â”€ Static well-known names â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    public static readonly PdfName Type             = new("Type");
    public static readonly PdfName Subtype          = new("Subtype");
    public static readonly PdfName Page             = new("Page");
    public static readonly PdfName Pages            = new("Pages");
    public static readonly PdfName Catalog          = new("Catalog");
    public static readonly PdfName Font             = new("Font");
    public static readonly PdfName Contents         = new("Contents");
    public static readonly PdfName Length           = new("Length");
    public static readonly PdfName Filter           = new("Filter");
    public static readonly PdfName Width            = new("Width");
    public static readonly PdfName Height           = new("Height");
    public static readonly PdfName BitsPerComponent = new("BitsPerComponent");
    public static readonly PdfName ColorSpace       = new("ColorSpace");
    public static readonly PdfName XObject          = new("XObject");
    public static readonly PdfName Image            = new("Image");
    public static readonly PdfName Resources        = new("Resources");
    public static readonly PdfName MediaBox         = new("MediaBox");
    public static readonly PdfName Parent           = new("Parent");
    public static readonly PdfName Kids             = new("Kids");
    public static readonly PdfName Count            = new("Count");
    public static readonly PdfName Root             = new("Root");
    public static readonly PdfName Size             = new("Size");
    public static readonly PdfName Info             = new("Info");
    public static readonly PdfName Creator          = new("Creator");
    public static readonly PdfName Producer         = new("Producer");
    public static readonly PdfName CreationDate     = new("CreationDate");
    public static readonly PdfName FlateDecode      = new("FlateDecode");
    public static readonly PdfName DCTDecode        = new("DCTDecode");
    public static readonly PdfName DeviceRGB        = new("DeviceRGB");
    public static readonly PdfName DeviceCMYK       = new("DeviceCMYK");
    public static readonly PdfName DeviceGray       = new("DeviceGray");
    public static readonly PdfName Encoding         = new("Encoding");
    public static readonly PdfName WinAnsiEncoding  = new("WinAnsiEncoding");
    public static readonly PdfName BaseFont         = new("BaseFont");
    public static readonly PdfName FirstChar        = new("FirstChar");
    public static readonly PdfName LastChar         = new("LastChar");
    public static readonly PdfName Widths           = new("Widths");
    public static readonly PdfName FontDescriptor   = new("FontDescriptor");
    public static readonly PdfName FontFile2        = new("FontFile2");
    public static readonly PdfName Flags            = new("Flags");
    public static readonly PdfName FontBBox         = new("FontBBox");
    public static readonly PdfName ItalicAngle      = new("ItalicAngle");
    public static readonly PdfName Ascent           = new("Ascent");
    public static readonly PdfName Descent          = new("Descent");
    public static readonly PdfName CapHeight        = new("CapHeight");
    public static readonly PdfName StemV            = new("StemV");
    public static readonly PdfName SMask            = new("SMask");
    public static readonly PdfName Matte            = new("Matte");
    public static readonly PdfName Transparency     = new("Transparency");
    public static readonly PdfName Group            = new("Group");
    public static readonly PdfName S                = new("S");
    public static readonly PdfName Title            = new("Title");
    public static readonly PdfName Author           = new("Author");
    public static readonly PdfName Subject          = new("Subject");
    public static readonly PdfName Keywords         = new("Keywords");
    public static readonly PdfName ModDate          = new("ModDate");
    public static readonly PdfName Indexed          = new("Indexed");
    public static readonly PdfName Annots           = new("Annots");
    public static readonly PdfName TrueType         = new("TrueType");
    public static readonly PdfName Type1            = new("Type1");
}

