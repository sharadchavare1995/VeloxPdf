namespace VeloxPdf.IO;

/// <summary>Static helper methods for PDF serialization tasks.</summary>
public static class PdfSerializer
{
    private static readonly Encoding _latin1 = Encoding.Latin1;

    /// <summary>
    /// Formats a float for PDF output: InvariantCulture, max 4 decimal places,
    /// no trailing zeros (e.g. 1.5 â†’ "1.5", not "1.5000").
    /// </summary>
    public static string FormatFloat(float f)
        => f.ToString("0.####", CultureInfo.InvariantCulture);

    /// <summary>
    /// Formats a DateTime as a PDF date string: D:YYYYMMDDHHmmSSOHH'mm'
    /// where O is the UTC offset sign (+ or -), or Z for UTC.
    /// </summary>
    public static string FormatDate(DateTime dt)
    {
        // Normalize to local time so we can capture the offset
        DateTime local  = dt.Kind == DateTimeKind.Utc ? dt.ToLocalTime() : dt;
        TimeSpan offset = TimeZoneInfo.Local.GetUtcOffset(local);

        char sign   = offset >= TimeSpan.Zero ? '+' : '-';
        TimeSpan abs = offset.Duration();

        return $"D:{local:yyyyMMddHHmmss}{sign}{abs.Hours:D2}'{abs.Minutes:D2}'";
    }

    /// <summary>
    /// Escapes a string for use inside PDF literal string parentheses.
    /// Escapes: ( ) \ \r \n
    /// </summary>
    public static string EscapePdfString(string s)
    {
        var sb = new StringBuilder(s.Length + 4);
        foreach (char c in s)
        {
            switch (c)
            {
                case '(':  sb.Append("\\("); break;
                case ')':  sb.Append("\\)"); break;
                case '\\': sb.Append("\\\\"); break;
                case '\r': sb.Append("\\r"); break;
                case '\n': sb.Append("\\n"); break;
                default:   sb.Append(c); break;
            }
        }
        return sb.ToString();
    }

    /// <summary>
    /// Escapes a PDF name: non-regular characters become #XX.
    /// The caller should not include the leading slash.
    /// </summary>
    public static string EscapePdfName(string name)
    {
        var sb = new StringBuilder(name.Length + 4);
        foreach (char c in name)
        {
            if (c > 0x20 && c < 0x7F &&
                c != '(' && c != ')' && c != '<' && c != '>' &&
                c != '[' && c != ']' && c != '{' && c != '}' &&
                c != '/' && c != '%' && c != '#')
            {
                sb.Append(c);
            }
            else
            {
                sb.Append($"#{(int)c:X2}");
            }
        }
        return sb.ToString();
    }

    /// <summary>Encodes a string using Latin-1 (ISO-8859-1) encoding.</summary>
    public static byte[] ToLatin1Bytes(string s) => _latin1.GetBytes(s);
}

