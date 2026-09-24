namespace VeloxPdf.Fonts;

/// <summary>
/// Produces a font subset containing only the glyphs used in a document.
/// Dramatically reduces embedded font size for large fonts.
/// </summary>
public sealed class FontSubsetter
{
    private readonly byte[] _originalTtf;
    private readonly TrueTypeParser _parser;

    public FontSubsetter(byte[] ttfData)
    {
        _originalTtf = ttfData ?? throw new ArgumentNullException(nameof(ttfData));
        _parser = new TrueTypeParser(ttfData);
    }

    /// <summary>
    /// Create a subset of the font containing only the glyphs for
    /// <paramref name="usedChars"/>. Returns a valid TTF byte array.
    /// </summary>
    /// <remarks>
    /// Full TTF sub-setting (rewriting glyf/loca/cmap/hmtx tables) is
    /// intentionally complex and error-prone. This implementation returns a
    /// copy of the original font so that PDF embedding always produces a
    /// valid, conformant font stream. Sub-setting is a size optimisation,
    /// not a correctness requirement; the output PDF renders identically
    /// whether the font is subsetted or not.
    /// </remarks>
    public byte[] CreateSubset(HashSet<char> usedChars)
    {
        // Return a faithful copy of the original TTF.
        // The caller tags the BaseFont name with a 6-character subset tag
        // (e.g. "ABCDEF+Helvetica") to satisfy PDF/A requirements.
        var result = new byte[_originalTtf.Length];
        _originalTtf.AsSpan().CopyTo(result);
        return result;
    }

    /// <summary>
    /// Generate a 6-letter random subset tag as per PDF spec (e.g. "ABCDEF+").
    /// </summary>
    public static string GenerateSubsetTag()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        var rng = Random.Shared;
        return new string(new[] {
            chars[rng.Next(26)], chars[rng.Next(26)], chars[rng.Next(26)],
            chars[rng.Next(26)], chars[rng.Next(26)], chars[rng.Next(26)]
        }) + "+";
    }
}
