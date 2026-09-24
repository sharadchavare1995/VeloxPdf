namespace VeloxPdf.Document;
using VeloxPdf.Primitives;
using VeloxPdf.IO;

public sealed class PdfMetadata
{
    public string Title { get; set; } = "";
    public string Author { get; set; } = "";
    public string Subject { get; set; } = "";
    public string Keywords { get; set; } = "";
    public string Creator { get; set; } = "VeloxPdf 1.0";
    public string Producer { get; set; } = "VeloxPdf 1.0 (https://github.com/momentivesoftware/VeloxPdf)";
    public DateTime CreationDate { get; set; } = DateTime.UtcNow;
    public DateTime? ModificationDate { get; set; }

    public PdfDictionary BuildInfoDictionary()
    {
        var dict = new PdfDictionary();
        if (!string.IsNullOrEmpty(Title))
            dict.Set("Title", new PdfString(Title));
        if (!string.IsNullOrEmpty(Author))
            dict.Set("Author", new PdfString(Author));
        if (!string.IsNullOrEmpty(Subject))
            dict.Set("Subject", new PdfString(Subject));
        if (!string.IsNullOrEmpty(Keywords))
            dict.Set("Keywords", new PdfString(Keywords));
        if (!string.IsNullOrEmpty(Creator))
            dict.Set("Creator", new PdfString(Creator));
        if (!string.IsNullOrEmpty(Producer))
            dict.Set("Producer", new PdfString(Producer));
        dict.Set("CreationDate", new PdfString(PdfSerializer.FormatDate(CreationDate)));
        if (ModificationDate.HasValue)
            dict.Set("ModDate", new PdfString(PdfSerializer.FormatDate(ModificationDate.Value)));
        return dict;
    }
}
