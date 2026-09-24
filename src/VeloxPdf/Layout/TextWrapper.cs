namespace VeloxPdf.Layout;

using VeloxPdf.Fonts;

public sealed record TextLine(string Content, float Width, bool IsLastInParagraph);

public static class TextWrapper
{
    public static IReadOnlyList<TextLine> WrapText(
        string text, float maxWidth, PdfFont font, float fontSize)
    {
        var lines = new List<TextLine>();
        if (string.IsNullOrEmpty(text))
        {
            lines.Add(new TextLine("", 0f, true));
            return lines;
        }

        // Split by explicit newlines first
        var paragraphs = text.Split('\n');
        for (int p = 0; p < paragraphs.Length; p++)
        {
            string paragraph = paragraphs[p];
            bool isLast = p == paragraphs.Length - 1;

            if (string.IsNullOrEmpty(paragraph))
            {
                lines.Add(new TextLine("", 0f, true));
                continue;
            }

            WrapParagraph(paragraph, maxWidth, font, fontSize, lines, isLast);
        }
        return lines;
    }

    private static void WrapParagraph(
        string text, float maxWidth, PdfFont font, float fontSize,
        List<TextLine> lines, bool isLastParagraph)
    {
        var words = text.Split(' ', StringSplitOptions.None);
        var currentLine = new System.Text.StringBuilder();
        float currentWidth = 0f;
        float spaceWidth = TextMeasurer.MeasureChar(' ', font, fontSize);

        for (int w = 0; w < words.Length; w++)
        {
            string word = words[w];
            if (string.IsNullOrEmpty(word))
            {
                // Preserve multiple spaces
                if (currentLine.Length > 0)
                {
                    currentLine.Append(' ');
                    currentWidth += spaceWidth;
                }
                continue;
            }

            float wordWidth = TextMeasurer.MeasureText(word, font, fontSize);

            if (currentLine.Length == 0)
            {
                // First word on line
                if (wordWidth > maxWidth)
                {
                    // Word is too long — force break it
                    ForceBreakWord(word, maxWidth, font, fontSize, lines);
                    continue;
                }
                currentLine.Append(word);
                currentWidth = wordWidth;
            }
            else
            {
                float needed = currentWidth + spaceWidth + wordWidth;
                if (needed <= maxWidth)
                {
                    currentLine.Append(' ');
                    currentLine.Append(word);
                    currentWidth = needed;
                }
                else
                {
                    // Flush current line
                    string lineText = currentLine.ToString();
                    lines.Add(new TextLine(lineText, currentWidth, false));
                    currentLine.Clear();

                    if (wordWidth > maxWidth)
                    {
                        ForceBreakWord(word, maxWidth, font, fontSize, lines);
                        currentWidth = 0f;
                    }
                    else
                    {
                        currentLine.Append(word);
                        currentWidth = wordWidth;
                    }
                }
            }
        }

        if (currentLine.Length > 0)
        {
            lines.Add(new TextLine(currentLine.ToString(), currentWidth, isLastParagraph));
        }
    }

    private static void ForceBreakWord(
        string word, float maxWidth, PdfFont font, float fontSize, List<TextLine> lines)
    {
        var part = new System.Text.StringBuilder();
        float partWidth = 0f;

        for (int i = 0; i < word.Length; i++)
        {
            float cw = TextMeasurer.MeasureChar(word[i], font, fontSize);
            if (partWidth + cw > maxWidth && part.Length > 0)
            {
                lines.Add(new TextLine(part.ToString(), partWidth, false));
                part.Clear();
                partWidth = 0f;
            }
            part.Append(word[i]);
            partWidth += cw;
        }
        if (part.Length > 0)
            lines.Add(new TextLine(part.ToString(), partWidth, false));
    }
}
