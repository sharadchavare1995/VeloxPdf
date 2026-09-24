namespace VeloxPdf.Layout;

using VeloxPdf.Fonts;

public static class TextMeasurer
{
    public static float MeasureText(string text, PdfFont font, float fontSize)
    {
        if (string.IsNullOrEmpty(text)) return 0f;
        return font.MeasureWidth(text.AsSpan(), fontSize);
    }

    public static float MeasureChar(char c, PdfFont font, float fontSize)
        => font.MeasureWidth(c, fontSize);

    public static float GetLineHeight(PdfFont font, float fontSize, float lineHeightMultiplier = 1.4f)
        => fontSize * lineHeightMultiplier;

    public static float GetAscender(PdfFont font, float fontSize)
        => font.Ascender(fontSize);

    public static float GetDescender(PdfFont font, float fontSize)
        => font.Descender(fontSize);
}
