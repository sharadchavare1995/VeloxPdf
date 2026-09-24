namespace VeloxPdf.Samples;
using VeloxPdf.Document;
using VeloxPdf.Templates;

public static class MultiTemplateSample
{
    /// <summary>
    /// Generates all three templates in parallel, demonstrating thread safety.
    /// </summary>
    public static async Task<(byte[] invoice, byte[] report, byte[] letter)> GenerateAllAsync()
    {
        var invoiceTask = Task.Run(InvoiceSample.Generate);
        var reportTask = Task.Run(ReportSample.Generate);
        var letterTask = Task.Run(GenerateLetter);

        await Task.WhenAll(invoiceTask, reportTask, letterTask);

        return (invoiceTask.Result, reportTask.Result, letterTask.Result);
    }

    private static byte[] GenerateLetter()
    {
        var data = new TemplateData
        {
            Title = "Business Partnership Letter",
            Metadata = new PdfMetadata
            {
                Title = "Partnership Proposal",
                Author = "John Smith"
            }
        };

        data.Properties["SenderName"] = "John Smith";
        data.Properties["SenderTitle"] = "Chief Executive Officer";
        data.Properties["SenderCompany"] = "Acme Corporation";
        data.Properties["SenderAddress"] = "1234 Business Ave, Suite 500";
        data.Properties["SenderCity"] = "New York, NY 10001";
        data.Properties["SenderEmail"] = "john.smith@acme.com";
        data.Properties["SenderPhone"] = "+1 (555) 123-4567";
        data.Properties["Date"] = "September 11, 2026";
        data.Properties["RecipientName"] = "Sarah Johnson";
        data.Properties["RecipientTitle"] = "Director of Partnerships";
        data.Properties["RecipientCompany"] = "TechVentures Inc.";
        data.Properties["RecipientAddress"] = "789 Innovation Drive";
        data.Properties["RecipientCity"] = "San Francisco, CA 94102";
        data.Properties["Subject"] = "Strategic Partnership Proposal - VeloxPDF Integration";
        data.Properties["Body"] =
            "I hope this letter finds you well. I am writing to express our strong interest in " +
            "establishing a strategic technology partnership between Acme Corporation and TechVentures Inc.\n\n" +
            "After carefully reviewing your platform capabilities and market position, we believe there " +
            "is a compelling opportunity for mutual benefit through the integration of our VeloxPDF " +
            "document generation technology with your enterprise workflow platform.\n\n" +
            "Our solution has demonstrated significant value for clients processing high-volume " +
            "documents, with benchmarks showing the ability to generate over 100,000 records " +
            "per second with minimal memory footprint. This capability aligns directly with the " +
            "scalability requirements we understand to be critical for your enterprise clients.\n\n" +
            "I would welcome the opportunity to schedule a technical demonstration at your earliest " +
            "convenience. Please feel free to contact me directly to arrange a suitable time.";
        data.Properties["Closing"] = "Yours sincerely,";
        data.Properties["SignerName"] = "John Smith";
        data.Properties["SignerTitle"] = "Chief Executive Officer, Acme Corporation";

        var template = new LetterTemplate();
        var doc = template.Build(data);
        return doc.Save();
    }
}
