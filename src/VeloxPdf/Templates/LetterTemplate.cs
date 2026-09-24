namespace VeloxPdf.Templates;
using VeloxPdf.Color;
using VeloxPdf.Document;
using VeloxPdf.Fonts;
using VeloxPdf.Layout;

public sealed class LetterTemplate : ITemplate
{
    public string Name => "Letter";
    public bool Supports(string name) =>
        string.Equals(name, Name, StringComparison.OrdinalIgnoreCase);

    public PdfDocument Build(TemplateData data)
    {
        var doc = new PdfDocument
        {
            Metadata = data.Metadata,
            DefaultFont = Type1Font.TimesRoman,
            DefaultFontSize = 11f
        };
        doc.Metadata.Title = data.Get("Subject", "Letter");

        string senderName    = data.Get("SenderName", "Your Name");
        string senderTitle   = data.Get("SenderTitle", "");
        string senderCompany = data.Get("SenderCompany", "");
        string senderAddress = data.Get("SenderAddress", "");
        string senderCity    = data.Get("SenderCity", "");
        string senderEmail   = data.Get("SenderEmail", "");
        string senderPhone   = data.Get("SenderPhone", "");
        string dateStr       = data.Get("Date", DateTime.Today.ToString("MMMM dd, yyyy"));
        string recipientName    = data.Get("RecipientName", "Sir/Madam");
        string recipientTitle   = data.Get("RecipientTitle", "");
        string recipientCompany = data.Get("RecipientCompany", "");
        string recipientAddress = data.Get("RecipientAddress", "");
        string recipientCity    = data.Get("RecipientCity", "");
        string subject   = data.Get("Subject", "");
        string body      = data.Get("Body", "I am writing to inform you of...\n\nPlease do not hesitate to contact us should you require further information.\n\nWe look forward to hearing from you.");
        string closing   = data.Get("Closing", "Sincerely,");
        string signerName  = data.Get("SignerName", senderName);
        string signerTitle = data.Get("SignerTitle", senderTitle);

        var page = doc.AddPage(PdfPageSize.Letter, PdfMargins.Symmetric(72, 72));
        var cs = page.ContentStream;
        string timesAlias     = page.RegisterFont(Type1Font.TimesRoman);
        string timesBoldAlias = page.RegisterFont(Type1Font.TimesBold);

        float pageH   = page.Height;
        float pageW   = page.Width;
        float ml      = page.Margins.Left;
        float mr      = page.Margins.Right;
        float mt      = page.Margins.Top;
        float contentW = pageW - ml - mr;

        // Letterhead band
        cs.SaveState();
        cs.SetFillColor(PdfColor.FromHex("#1E3A8A"));
        cs.Rectangle(0, pageH - 60, pageW, 60);
        cs.Fill();
        cs.RestoreState();

        if (!string.IsNullOrEmpty(senderCompany))
        {
            cs.BeginText();
            cs.SetFont(timesBoldAlias, 16);
            cs.SetFillColor(PdfColor.White);
            cs.SetAbsolutePos(ml, pageH - 38);
            cs.ShowText(senderCompany);
            cs.EndText();
        }

        float curY = pageH - mt - 30;

        // Sender block (top right)
        float senderX = pageW - mr - 200;
        RenderBlock(cs, timesAlias, senderX, curY, 9f, PdfColor.FromHex("#374151"), new[]
        {
            senderName, senderTitle, senderAddress, senderCity, senderEmail, senderPhone
        });
        curY -= 80;

        // Date
        cs.BeginText();
        cs.SetFont(timesAlias, 11);
        cs.SetFillColor(PdfColor.FromHex("#111827"));
        cs.SetAbsolutePos(ml, curY);
        cs.ShowText(dateStr);
        cs.EndText();
        curY -= 30;

        // Recipient block
        RenderBlock(cs, timesAlias, ml, curY, 11f, PdfColor.FromHex("#111827"), new[]
        {
            recipientName, recipientTitle, recipientCompany, recipientAddress, recipientCity
        });
        curY -= 80;

        // Subject line
        if (!string.IsNullOrEmpty(subject))
        {
            cs.BeginText();
            cs.SetFont(timesBoldAlias, 11);
            cs.SetFillColor(PdfColor.FromHex("#111827"));
            cs.SetAbsolutePos(ml, curY);
            cs.ShowText("Re: " + subject);
            cs.EndText();
            curY -= 20;
        }

        // Salutation
        cs.BeginText();
        cs.SetFont(timesAlias, 11);
        cs.SetFillColor(PdfColor.FromHex("#111827"));
        cs.SetAbsolutePos(ml, curY);
        cs.ShowText($"Dear {recipientName},");
        cs.EndText();
        curY -= 20;

        // Body paragraphs
        var paragraphs = body.Split(new[] { "\n\n", "\r\n\r\n" }, StringSplitOptions.None);
        foreach (var para in paragraphs)
        {
            var trimmed = para.Trim();
            if (string.IsNullOrEmpty(trimmed)) { curY -= 8; continue; }

            var lines = TextWrapper.WrapText(trimmed, contentW, Type1Font.TimesRoman, 11f);
            foreach (var line in lines)
            {
                if (curY < 100) break;
                cs.BeginText();
                cs.SetFont(timesAlias, 11);
                cs.SetFillColor(PdfColor.FromHex("#111827"));
                cs.SetAbsolutePos(ml, curY);
                cs.ShowText(line.Content);
                cs.EndText();
                curY -= 16;
            }
            curY -= 8;
        }

        curY -= 15;

        // Closing
        cs.BeginText();
        cs.SetFont(timesAlias, 11);
        cs.SetFillColor(PdfColor.FromHex("#111827"));
        cs.SetAbsolutePos(ml, curY);
        cs.ShowText(closing);
        cs.EndText();
        curY -= 50;

        // Signature line
        cs.SaveState();
        cs.SetStrokeColor(PdfColor.FromHex("#374151"));
        cs.SetLineWidth(0.5f);
        cs.MoveTo(ml, curY);
        cs.LineTo(ml + 180, curY);
        cs.Stroke();
        cs.RestoreState();
        curY -= 14;

        cs.BeginText();
        cs.SetFont(timesBoldAlias, 11);
        cs.SetFillColor(PdfColor.FromHex("#111827"));
        cs.SetAbsolutePos(ml, curY);
        cs.ShowText(signerName);
        cs.EndText();

        if (!string.IsNullOrEmpty(signerTitle))
        {
            curY -= 14;
            cs.BeginText();
            cs.SetFont(timesAlias, 10);
            cs.SetFillColor(PdfColor.FromHex("#6B7280"));
            cs.SetAbsolutePos(ml, curY);
            cs.ShowText(signerTitle);
            cs.EndText();
        }

        return doc;
    }

    private static void RenderBlock(Graphics.ContentStream cs, string fontAlias,
        float x, float y, float fontSize, PdfColor color, string[] lines)
    {
        float lineH = fontSize * 1.4f;
        int written = 0;
        for (int i = 0; i < lines.Length; i++)
        {
            if (string.IsNullOrEmpty(lines[i])) continue;
            cs.BeginText();
            cs.SetFont(fontAlias, fontSize);
            cs.SetFillColor(color);
            cs.SetAbsolutePos(x, y - written * lineH);
            cs.ShowText(lines[i]);
            cs.EndText();
            written++;
        }
    }
}
