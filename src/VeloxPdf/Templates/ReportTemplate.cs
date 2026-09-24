namespace VeloxPdf.Templates;
using VeloxPdf.Color;
using VeloxPdf.Document;
using VeloxPdf.Fonts;
using VeloxPdf.Layout;

public record ReportSection(string Title, string Body);

public sealed class ReportTemplate : ITemplate
{
    public string Name => "Report";
    public bool Supports(string name) =>
        string.Equals(name, Name, StringComparison.OrdinalIgnoreCase);

    public PdfDocument Build(TemplateData data)
    {
        var doc = new PdfDocument
        {
            Metadata = data.Metadata,
            DefaultFont = Type1Font.Helvetica,
            DefaultFontSize = 10f,
            DefaultPageSize = PdfPageSize.A4,
            DefaultMargins = PdfMargins.Default
        };
        doc.Metadata.Title = data.Title;

        string reportTitle = data.Get("ReportTitle", data.Title);
        string author = data.Get("Author", doc.Metadata.Author);
        string date = data.Get("Date", DateTime.Today.ToString("MMMM dd, yyyy"));
        var sections = data.Get<List<ReportSection>>("Sections", new List<ReportSection>
        {
            new("Executive Summary", "This report provides a comprehensive overview of the topic at hand. The following pages detail our findings and recommendations based on thorough analysis."),
            new("Methodology", "The following methodology was employed in this analysis. Data was collected from multiple sources and verified for accuracy before inclusion in this report."),
            new("Findings", "Our analysis revealed the following key findings. These results have significant implications for the organization and its future strategy.")
        });

        // === COVER PAGE ===
        var coverPage = doc.AddPage();
        var cs = coverPage.ContentStream;
        string boldAlias = coverPage.RegisterFont(Type1Font.HelveticaBold);
        string helvAlias = coverPage.RegisterFont(Type1Font.Helvetica);

        float pageH = coverPage.Height;
        float pageW = coverPage.Width;
        float ml = coverPage.Margins.Left;
        float mr = coverPage.Margins.Right;
        float contentW = pageW - ml - mr;

        // Cover header band
        cs.SaveState();
        cs.SetFillColor(PdfColor.FromHex("#1E3A8A"));
        cs.Rectangle(0, pageH - 120, pageW, 120);
        cs.Fill();
        cs.RestoreState();

        cs.BeginText();
        cs.SetFont(boldAlias, 26);
        cs.SetFillColor(PdfColor.White);
        cs.SetAbsolutePos(ml, pageH - 65);
        cs.ShowText(reportTitle);
        cs.EndText();

        cs.BeginText();
        cs.SetFont(helvAlias, 11);
        cs.SetFillColor(PdfColor.FromHex("#BFDBFE"));
        cs.SetAbsolutePos(ml, pageH - 90);
        cs.ShowText("REPORT");
        cs.EndText();

        float detailY = pageH / 2f;
        cs.BeginText();
        cs.SetFont(boldAlias, 12);
        cs.SetFillColor(PdfColor.FromHex("#374151"));
        cs.SetAbsolutePos(ml, detailY + 20);
        cs.ShowText("Prepared by: " + author);
        cs.EndText();

        cs.BeginText();
        cs.SetFont(helvAlias, 10);
        cs.SetFillColor(PdfColor.FromHex("#6B7280"));
        cs.SetAbsolutePos(ml, detailY);
        cs.ShowText("Date: " + date);
        cs.EndText();

        // Table of contents
        cs.BeginText();
        cs.SetFont(boldAlias, 14);
        cs.SetFillColor(PdfColor.FromHex("#1E3A8A"));
        cs.SetAbsolutePos(ml, detailY - 50);
        cs.ShowText("Contents");
        cs.EndText();

        float tocY = detailY - 70;
        for (int i = 0; i < sections.Count; i++)
        {
            cs.BeginText();
            cs.SetFont(helvAlias, 10);
            cs.SetFillColor(PdfColor.FromHex("#374151"));
            cs.SetAbsolutePos(ml, tocY - i * 18);
            cs.ShowText($"{i + 1}.  {sections[i].Title}");
            cs.SetAbsolutePos(pageW - mr - 30, tocY - i * 18);
            cs.ShowText($"{i + 2}");
            cs.EndText();

            cs.SaveState();
            cs.SetStrokeColor(PdfColor.FromHex("#D1D5DB"));
            cs.SetLineWidth(0.5f);
            cs.SetDash([2f, 4f], 0);
            cs.MoveTo(ml + 150, tocY - i * 18 + 3);
            cs.LineTo(pageW - mr - 35, tocY - i * 18 + 3);
            cs.Stroke();
            cs.RestoreState();
        }

        // === CONTENT PAGES ===
        int totalPages = sections.Count + 1;
        for (int s = 0; s < sections.Count; s++)
        {
            var section = sections[s];
            var contentPage = doc.AddPage();
            var pcs = contentPage.ContentStream;
            string pbold = contentPage.RegisterFont(Type1Font.HelveticaBold);
            string phelv = contentPage.RegisterFont(Type1Font.Helvetica);
            int pageNum = s + 2;

            float cpH = contentPage.Height;
            float cpW = contentPage.Width;
            float cml = contentPage.Margins.Left;
            float cmr = contentPage.Margins.Right;
            float cmt = contentPage.Margins.Top;
            float cmb = contentPage.Margins.Bottom;
            float cContentW = cpW - cml - cmr;

            // Running header
            pcs.SaveState();
            pcs.SetFillColor(PdfColor.FromHex("#F3F4F6"));
            pcs.Rectangle(0, cpH - 30, cpW, 30);
            pcs.Fill();
            pcs.SetStrokeColor(PdfColor.FromHex("#E5E7EB"));
            pcs.SetLineWidth(0.5f);
            pcs.MoveTo(0, cpH - 30);
            pcs.LineTo(cpW, cpH - 30);
            pcs.Stroke();
            pcs.RestoreState();

            pcs.BeginText();
            pcs.SetFont(phelv, 8);
            pcs.SetFillColor(PdfColor.FromHex("#6B7280"));
            pcs.SetAbsolutePos(cml, cpH - 20);
            pcs.ShowText(reportTitle);
            pcs.SetAbsolutePos(cpW - cmr - 70, cpH - 20);
            pcs.ShowText($"Page {pageNum} of {totalPages}");
            pcs.EndText();

            // Section heading
            float contentY = cpH - cmt - 15;
            pcs.BeginText();
            pcs.SetFont(pbold, 16);
            pcs.SetFillColor(PdfColor.FromHex("#1E3A8A"));
            pcs.SetAbsolutePos(cml, contentY);
            pcs.ShowText($"{s + 1}. {section.Title}");
            pcs.EndText();
            contentY -= 10;

            pcs.SaveState();
            pcs.SetStrokeColor(PdfColor.FromHex("#2563EB"));
            pcs.SetLineWidth(1.5f);
            pcs.MoveTo(cml, contentY);
            pcs.LineTo(cml + 200, contentY);
            pcs.Stroke();
            pcs.RestoreState();
            contentY -= 20;

            // Body text
            var bodyFont = Type1Font.Helvetica;
            float bodyFontSize = 10f;
            var lines = TextWrapper.WrapText(section.Body, cContentW, bodyFont, bodyFontSize);
            float lineH = bodyFontSize * 1.4f;

            foreach (var line in lines)
            {
                if (contentY < cmb + 20) break;
                pcs.BeginText();
                pcs.SetFont(phelv, bodyFontSize);
                pcs.SetFillColor(PdfColor.FromHex("#374151"));
                pcs.SetAbsolutePos(cml, contentY);
                pcs.ShowText(line.Content);
                pcs.EndText();
                contentY -= lineH;
            }

            // Footer
            pcs.SaveState();
            pcs.SetStrokeColor(PdfColor.FromHex("#E5E7EB"));
            pcs.SetLineWidth(0.5f);
            pcs.MoveTo(cml, 40);
            pcs.LineTo(cpW - cmr, 40);
            pcs.Stroke();
            pcs.RestoreState();

            pcs.BeginText();
            pcs.SetFont(phelv, 8);
            pcs.SetFillColor(PdfColor.FromHex("#9CA3AF"));
            pcs.SetAbsolutePos(cpW / 2f - 30, 28);
            pcs.ShowText($"Page {pageNum} of {totalPages}");
            pcs.EndText();
        }

        return doc;
    }
}
