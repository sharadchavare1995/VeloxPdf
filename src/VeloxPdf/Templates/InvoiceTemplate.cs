namespace VeloxPdf.Templates;
using VeloxPdf.Color;
using VeloxPdf.Document;
using VeloxPdf.Elements;
using VeloxPdf.Fonts;
using VeloxPdf.Graphics;
using VeloxPdf.Styling;

public sealed class InvoiceTemplate : ITemplate
{
    public string Name => "Invoice";
    public bool Supports(string templateName) =>
        string.Equals(templateName, Name, StringComparison.OrdinalIgnoreCase);

    private static readonly PdfColor PrimaryBlue = PdfColor.FromHex("#1E3A8A");
    private static readonly PdfColor AccentBlue = PdfColor.FromHex("#2563EB");
    private static readonly PdfColor LightGray = PdfColor.FromHex("#F9FAFB");
    private static readonly PdfColor BorderGray = PdfColor.FromHex("#D1D5DB");
    private static readonly PdfColor DarkText = PdfColor.FromHex("#111827");
    private static readonly PdfColor MedText = PdfColor.FromHex("#6B7280");

    public PdfDocument Build(TemplateData data)
    {
        var doc = new PdfDocument
        {
            Metadata = data.Metadata,
            DefaultFont = Type1Font.Helvetica,
            DefaultFontSize = 10f
        };
        doc.Metadata.Title = data.Get("InvoiceNumber", "INVOICE");

        string companyName = data.Get("CompanyName", "Your Company Name");
        string companyAddress = data.Get("CompanyAddress", "123 Main St, City, State 12345");
        string companyEmail = data.Get("CompanyEmail", "billing@company.com");
        string invoiceNumber = data.Get("InvoiceNumber", "INV-001");
        string invoiceDate = data.Get("InvoiceDate", DateTime.Today.ToString("MMM dd, yyyy"));
        string dueDate = data.Get("DueDate", DateTime.Today.AddDays(30).ToString("MMM dd, yyyy"));
        string billToName = data.Get("BillToName", "Client Name");
        string billToAddress = data.Get("BillToAddress", "456 Client Ave, City, ST 67890");
        string billToEmail = data.Get("BillToEmail", "client@example.com");

        var lineItems = data.Get<List<string[]>>("LineItems", new List<string[]>
        {
            new string[] { "Professional Services", "10", "150.00" },
            new string[] { "Software License", "1", "500.00" },
            new string[] { "Support & Maintenance", "1", "200.00" }
        });

        decimal subtotal = 0;
        foreach (var item in lineItems)
        {
            if (item.Length >= 3 &&
                int.TryParse(item[1], out int qty) &&
                decimal.TryParse(item[2], System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out decimal price))
                subtotal += qty * price;
        }
        decimal taxRate = data.Get<decimal>("TaxRate", 0.10m);
        decimal tax = subtotal * taxRate;
        decimal total = subtotal + tax;

        string taxLabel = data.Get("TaxLabel", "Tax (10%)");
        string paymentTerms = data.Get("PaymentTerms", "Net 30 days");
        string thankYouNote = data.Get("ThankYouNote", "Thank you for your business!");

        var page = doc.AddPage(PdfPageSize.A4, PdfMargins.Default);
        var cs = page.ContentStream;

        string helvAlias = page.RegisterFont(Type1Font.Helvetica);
        string boldAlias = page.RegisterFont(Type1Font.HelveticaBold);

        float pageW = page.Width;
        float pageH = page.Height;
        float marginL = page.Margins.Left;
        float marginR = page.Margins.Right;
        float contentW = pageW - marginL - marginR;
        float curY = page.Margins.Top;

        // Header background
        cs.SaveState();
        cs.SetFillColor(PrimaryBlue);
        cs.Rectangle(0, pageH - 100, pageW, 100);
        cs.Fill();
        cs.RestoreState();

        // Company name
        cs.BeginText();
        cs.SetFont(boldAlias, 22);
        cs.SetFillColor(PdfColor.White);
        cs.SetAbsolutePos(marginL, pageH - 45);
        cs.ShowText(companyName);
        cs.EndText();

        // Company address
        cs.BeginText();
        cs.SetFont(helvAlias, 9);
        cs.SetFillColor(PdfColor.FromHex("#BFDBFE"));
        cs.SetAbsolutePos(marginL, pageH - 65);
        cs.ShowText(companyAddress + " | " + companyEmail);
        cs.EndText();

        // INVOICE label
        cs.BeginText();
        cs.SetFont(boldAlias, 28);
        cs.SetFillColor(PdfColor.White);
        cs.SetAbsolutePos(pageW - marginR - 120, pageH - 50);
        cs.ShowText("INVOICE");
        cs.EndText();

        curY = 115f;

        float rightCol = pageW - marginR - 200;
        RenderLabelValue(cs, helvAlias, boldAlias, rightCol + 10, pageH - curY, "Invoice #:", invoiceNumber, MedText, DarkText);
        RenderLabelValue(cs, helvAlias, boldAlias, rightCol + 10, pageH - curY - 16, "Date:", invoiceDate, MedText, DarkText);
        RenderLabelValue(cs, helvAlias, boldAlias, rightCol + 10, pageH - curY - 32, "Due Date:", dueDate, MedText, DarkText);

        curY += 20;
        cs.SaveState();
        cs.SetStrokeColor(BorderGray);
        cs.SetLineWidth(0.5f);
        cs.MoveTo(marginL, pageH - curY);
        cs.LineTo(pageW - marginR, pageH - curY);
        cs.Stroke();
        cs.RestoreState();
        curY += 15;

        cs.BeginText();
        cs.SetFont(boldAlias, 9);
        cs.SetFillColor(MedText);
        cs.SetAbsolutePos(marginL, pageH - curY);
        cs.ShowText("BILL TO");
        cs.EndText();

        curY += 14;
        cs.BeginText();
        cs.SetFont(boldAlias, 11);
        cs.SetFillColor(DarkText);
        cs.SetAbsolutePos(marginL, pageH - curY);
        cs.ShowText(billToName);
        cs.EndText();

        curY += 14;
        cs.BeginText();
        cs.SetFont(helvAlias, 9);
        cs.SetFillColor(MedText);
        cs.SetAbsolutePos(marginL, pageH - curY);
        cs.ShowText(billToAddress);
        cs.EndText();

        curY += 12;
        cs.BeginText();
        cs.SetFont(helvAlias, 9);
        cs.SetFillColor(MedText);
        cs.SetAbsolutePos(marginL, pageH - curY);
        cs.ShowText(billToEmail);
        cs.EndText();

        curY += 25;

        float[] colW = [contentW * 0.45f, contentW * 0.12f, contentW * 0.20f, contentW * 0.23f];
        string[] headers = ["Description", "Qty", "Unit Price", "Amount"];
        float headerH = 22f;
        float tableX = marginL;

        cs.SaveState();
        cs.SetFillColor(AccentBlue);
        cs.Rectangle(tableX, pageH - curY - headerH, contentW, headerH);
        cs.Fill();
        cs.RestoreState();

        float hx = tableX;
        for (int c = 0; c < headers.Length; c++)
        {
            cs.BeginText();
            cs.SetFont(boldAlias, 9);
            cs.SetFillColor(PdfColor.White);
            cs.SetAbsolutePos(hx + 5, pageH - curY - 15);
            cs.ShowText(headers[c]);
            cs.EndText();
            hx += colW[c];
        }
        curY += headerH;

        int rowNum = 0;
        foreach (var item in lineItems)
        {
            float rowH = 18f;
            bool isOdd = rowNum % 2 == 1;

            if (isOdd)
            {
                cs.SaveState();
                cs.SetFillColor(LightGray);
                cs.Rectangle(tableX, pageH - curY - rowH, contentW, rowH);
                cs.Fill();
                cs.RestoreState();
            }

            string desc = item.Length > 0 ? item[0] : "";
            string qty = item.Length > 1 ? item[1] : "1";
            string unitPrice = item.Length > 2 ? item[2] : "0.00";
            decimal amount = 0;
            if (int.TryParse(qty, out int q) &&
                decimal.TryParse(unitPrice, System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out decimal up))
                amount = q * up;

            string[] cellValues = [desc, qty, $"${unitPrice}", $"${amount:N2}"];
            float cx = tableX;
            for (int c = 0; c < cellValues.Length; c++)
            {
                cs.BeginText();
                cs.SetFont(helvAlias, 9);
                cs.SetFillColor(DarkText);
                cs.SetAbsolutePos(cx + 5, pageH - curY - 13);
                cs.ShowText(cellValues[c]);
                cs.EndText();
                cx += colW[c];
            }

            cs.SaveState();
            cs.SetStrokeColor(BorderGray);
            cs.SetLineWidth(0.3f);
            cs.MoveTo(tableX, pageH - curY - rowH);
            cs.LineTo(tableX + contentW, pageH - curY - rowH);
            cs.Stroke();
            cs.RestoreState();

            curY += rowH;
            rowNum++;
        }

        curY += 10;

        float summaryX = tableX + contentW * 0.55f;
        float summaryW = contentW * 0.45f;
        RenderSummaryLine(cs, helvAlias, boldAlias, summaryX, pageH - curY, summaryW,
            "Subtotal", $"${subtotal:N2}", false);
        curY += 16;
        RenderSummaryLine(cs, helvAlias, boldAlias, summaryX, pageH - curY, summaryW,
            taxLabel, $"${tax:N2}", false);
        curY += 16;

        cs.SaveState();
        cs.SetFillColor(PrimaryBlue);
        cs.Rectangle(summaryX, pageH - curY - 4, summaryW, 22);
        cs.Fill();
        cs.RestoreState();
        RenderSummaryLine(cs, helvAlias, boldAlias, summaryX, pageH - curY + 10, summaryW,
            "TOTAL", $"${total:N2}", true, PdfColor.White);
        curY += 30;

        float footerY = 60f;
        cs.SaveState();
        cs.SetStrokeColor(BorderGray);
        cs.SetLineWidth(0.5f);
        cs.MoveTo(marginL, footerY + 20);
        cs.LineTo(pageW - marginR, footerY + 20);
        cs.Stroke();
        cs.RestoreState();

        cs.BeginText();
        cs.SetFont(boldAlias, 9);
        cs.SetFillColor(DarkText);
        cs.SetAbsolutePos(marginL, footerY + 8);
        cs.ShowText("Payment Terms: " + paymentTerms);
        cs.EndText();

        cs.BeginText();
        cs.SetFont(helvAlias, 9);
        cs.SetFillColor(MedText);
        cs.SetAbsolutePos(marginL, footerY - 6);
        cs.ShowText(thankYouNote);
        cs.EndText();

        cs.BeginText();
        cs.SetFont(helvAlias, 8);
        cs.SetFillColor(MedText);
        cs.SetAbsolutePos(pageW - marginR - 40, footerY - 6);
        cs.ShowText("Page 1");
        cs.EndText();

        return doc;
    }

    private static void RenderLabelValue(ContentStream cs, string helvAlias, string boldAlias,
        float x, float y, string label, string value, PdfColor labelColor, PdfColor valueColor)
    {
        cs.BeginText();
        cs.SetFont(helvAlias, 9);
        cs.SetFillColor(labelColor);
        cs.SetAbsolutePos(x, y);
        cs.ShowText(label);
        cs.EndText();

        cs.BeginText();
        cs.SetFont(boldAlias, 9);
        cs.SetFillColor(valueColor);
        cs.SetAbsolutePos(x + 55, y);
        cs.ShowText(value);
        cs.EndText();
    }

    private static void RenderSummaryLine(ContentStream cs, string helvAlias, string boldAlias,
        float x, float y, float width, string label, string value, bool bold,
        PdfColor? color = null)
    {
        var c = color ?? PdfColor.FromHex("#111827");
        cs.BeginText();
        cs.SetFont(bold ? boldAlias : helvAlias, 9);
        cs.SetFillColor(c);
        cs.SetAbsolutePos(x + 5, y);
        cs.ShowText(label);
        cs.SetAbsolutePos(x + width - 60, y);
        cs.ShowText(value);
        cs.EndText();
    }
}
