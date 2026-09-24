namespace VeloxPdf.Color;

/// <summary>Parses color values from string representations.</summary>
public static class ColorParser
{
    private static readonly Dictionary<string, PdfColor> _namedColors =
        new(StringComparer.OrdinalIgnoreCase)
        {
            { "black",       PdfColor.Black },
            { "white",       PdfColor.White },
            { "red",         PdfColor.Red },
            { "green",       PdfColor.Green },
            { "blue",        PdfColor.Blue },
            { "gray",        PdfColor.Gray },
            { "grey",        PdfColor.Gray },
            { "lightgray",   PdfColor.LightGray },
            { "lightgrey",   PdfColor.LightGray },
            { "darkgray",    PdfColor.DarkGray },
            { "darkgrey",    PdfColor.DarkGray },
            { "orange",      PdfColor.Orange },
            { "yellow",      PdfColor.Yellow },
            { "purple",      PdfColor.Purple },
            { "cyan",        PdfColor.Cyan },
            { "magenta",     PdfColor.Magenta },
            { "brown",       PdfColor.Brown },
            { "navy",        PdfColor.Navy },
            { "teal",        PdfColor.Teal },
            { "silver",      PdfColor.Silver },
            { "gold",        PdfColor.Gold },
            { "transparent", PdfColor.Transparent },
            { "primaryblue", PdfColor.PrimaryBlue },
            { "darkblue",    PdfColor.DarkBlue },
            { "lime",        new RgbColor(0f, 1f, 0f) },
            { "maroon",      new RgbColor(0.502f, 0f, 0f) },
            { "olive",       new RgbColor(0.502f, 0.502f, 0f) },
            { "aqua",        new RgbColor(0f, 1f, 1f) },
            { "fuchsia",     new RgbColor(1f, 0f, 1f) },
            { "coral",       new RgbColor(1f, 0.498f, 0.314f) },
            { "salmon",      new RgbColor(0.98f, 0.502f, 0.447f) },
            { "indigo",      new RgbColor(0.294f, 0f, 0.51f) },
        };

    /// <summary>Parses a color from a string. Throws on failure.</summary>
    public static PdfColor Parse(string input)
    {
        if (TryParse(input, out var color) && color != null)
            return color;
        throw new FormatException($"Unable to parse color: '{input}'");
    }

    /// <summary>Attempts to parse a color from a string.</summary>
    public static bool TryParse(string? input, out PdfColor? color)
    {
        color = null;
        if (string.IsNullOrWhiteSpace(input)) return false;

        input = input.Trim();

        if (input.StartsWith('#'))
        {
            try { color = ParseHex(input); return true; }
            catch { return false; }
        }

        if (input.StartsWith("rgb(", StringComparison.OrdinalIgnoreCase) && input.EndsWith(')'))
        {
            try { color = ParseRgbFunction(input); return true; }
            catch { return false; }
        }

        if (input.StartsWith("cmyk(", StringComparison.OrdinalIgnoreCase) && input.EndsWith(')'))
        {
            try { color = ParseCmykFunction(input); return true; }
            catch { return false; }
        }

        if (_namedColors.TryGetValue(input, out var named))
        {
            color = named;
            return true;
        }

        return false;
    }

    /// <summary>Parses a named color (case-insensitive).</summary>
    public static PdfColor ParseNamed(string name)
    {
        if (_namedColors.TryGetValue(name, out var c)) return c;
        throw new FormatException($"Unknown color name: '{name}'");
    }

    /// <summary>Parses a hex color string (#RGB or #RRGGBB).</summary>
    public static PdfColor ParseHex(string hex) => PdfColor.FromHex(hex);

    // ── Private helpers ───────────────────────────────────────────────────────

    private static PdfColor ParseRgbFunction(string input)
    {
        // rgb(r,g,b)  values 0-255
        var inner = input[4..^1];
        var parts = inner.Split(',');
        if (parts.Length != 3) throw new FormatException($"Invalid rgb() color: {input}");
        byte r = byte.Parse(parts[0].Trim());
        byte g = byte.Parse(parts[1].Trim());
        byte b = byte.Parse(parts[2].Trim());
        return PdfColor.FromRgb(r, g, b);
    }

    private static PdfColor ParseCmykFunction(string input)
    {
        var inner = input[5..^1];
        var parts = inner.Split(',');
        if (parts.Length != 4) throw new FormatException($"Invalid cmyk() color: {input}");
        float c = float.Parse(parts[0].Trim(), System.Globalization.CultureInfo.InvariantCulture);
        float m = float.Parse(parts[1].Trim(), System.Globalization.CultureInfo.InvariantCulture);
        float y = float.Parse(parts[2].Trim(), System.Globalization.CultureInfo.InvariantCulture);
        float k = float.Parse(parts[3].Trim(), System.Globalization.CultureInfo.InvariantCulture);
        return PdfColor.FromCmyk(c, m, y, k);
    }
}
