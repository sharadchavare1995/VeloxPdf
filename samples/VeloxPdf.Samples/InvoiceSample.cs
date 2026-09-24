namespace VeloxPdf.Samples;
using VeloxPdf.Document;
using VeloxPdf.Templates;

public static class InvoiceSample
{
    public static byte[] Generate()
    {
        var data = new TemplateData
        {
            Title = "INV-2024-001",
            Metadata = new PdfMetadata
            {
                Title = "Invoice INV-2024-001",
                Author = "Acme Corporation",
                Subject = "Professional Services Invoice"
            }
        };

        data.Properties["CompanyName"] = "Acme Corporation";
        data.Properties["CompanyAddress"] = "1234 Business Ave, Suite 500, New York, NY 10001";
        data.Properties["CompanyEmail"] = "billing@acme.com";
        data.Properties["InvoiceNumber"] = "INV-2024-001";
        data.Properties["InvoiceDate"] = "September 11, 2026";
        data.Properties["DueDate"] = "October 11, 2026";
        data.Properties["BillToName"] = "Tech Innovations Ltd";
        data.Properties["BillToAddress"] = "456 Client Street, San Francisco, CA 94102";
        data.Properties["BillToEmail"] = "accounts@techinnovations.com";
        data.Properties["TaxLabel"] = "Sales Tax (8.5%)";
        data.Properties["PaymentTerms"] = "Net 30 days. Late payments subject to 1.5% monthly interest.";
        data.Properties["ThankYouNote"] = "Thank you for choosing Acme Corporation. We value your business!";

        var lineItems = new List<string[]>
        {
            new string[] { "Software Development Services", "40", "175.00" },
            new string[] { "UI/UX Design Consultation", "10", "150.00" },
            new string[] { "Cloud Infrastructure Setup", "1", "2500.00" },
            new string[] { "Technical Documentation", "8", "125.00" },
            new string[] { "Project Management", "20", "100.00" }
        };
        data.Data["LineItems"] = lineItems;
        data.Data["TaxRate"] = 0.085m;

        var template = new InvoiceTemplate();
        var doc = template.Build(data);
        return doc.Save();
    }
}
