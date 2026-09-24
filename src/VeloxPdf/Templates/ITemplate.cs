namespace VeloxPdf.Templates;
using VeloxPdf.Document;

public interface ITemplate
{
    string Name { get; }
    PdfDocument Build(TemplateData data);
    bool Supports(string templateName);
}
