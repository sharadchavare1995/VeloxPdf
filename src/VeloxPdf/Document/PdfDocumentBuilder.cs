namespace VeloxPdf.Document;
using VeloxPdf.Elements;
using VeloxPdf.Fonts;
using VeloxPdf.Images;
using VeloxPdf.Styling;

public sealed class PdfDocumentBuilder
{
    private readonly PdfDocument _doc = new();

    public static PdfDocumentBuilder Create() => new();

    public PdfDocumentBuilder WithTitle(string title)
    { _doc.Metadata.Title = title; return this; }

    public PdfDocumentBuilder WithAuthor(string author)
    { _doc.Metadata.Author = author; return this; }

    public PdfDocumentBuilder WithSubject(string subject)
    { _doc.Metadata.Subject = subject; return this; }

    public PdfDocumentBuilder WithPageSize(PdfPageSize size)
    { _doc.DefaultPageSize = size; return this; }

    public PdfDocumentBuilder WithMargins(PdfMargins margins)
    { _doc.DefaultMargins = margins; return this; }

    public PdfDocumentBuilder WithDefaultFont(PdfFont font)
    { _doc.DefaultFont = font; return this; }

    public PdfDocumentBuilder WithDefaultFontSize(float size)
    { _doc.DefaultFontSize = size; return this; }

    public PdfDocumentBuilder WithMetadata(Action<PdfMetadata> configure)
    { configure(_doc.Metadata); return this; }

    public PdfDocumentBuilder AddText(string text, TextStyle? style = null)
    {
        _doc.Elements.Add(new TextElement { Text = text, Style = style ?? TextStyle.Default });
        return this;
    }

    public PdfDocumentBuilder AddHeading(string text, int level = 1)
    {
        var style = level switch
        {
            1 => TextStyle.Heading1,
            2 => TextStyle.Heading2,
            3 => TextStyle.Heading3,
            _ => TextStyle.Default
        };
        _doc.Elements.Add(new TextElement { Text = text, Style = style });
        return this;
    }

    public PdfDocumentBuilder AddPageBreak()
    { _doc.Elements.Add(new PageBreakElement()); return this; }

    public PdfDocumentBuilder AddLine()
    { _doc.Elements.Add(new LineElement()); return this; }

    public PdfDocumentBuilder AddSpace(float points)
    {
        _doc.Elements.Add(new TextElement
        {
            Text = " ",
            Style = new TextStyle { SpaceBefore = points, SpaceAfter = 0, FontSize = 1 }
        });
        return this;
    }

    public PdfDocumentBuilder AddTable(Action<TableBuilder> configure)
    {
        var builder = new TableBuilder();
        configure(builder);
        _doc.Elements.Add(builder.Build());
        return this;
    }

    public PdfDocumentBuilder AddImage(PdfImage image, PdfAlignment alignment = PdfAlignment.Left)
    {
        _doc.Elements.Add(new ImageElement(image) { HorizontalAlignment = alignment });
        return this;
    }

    public PdfDocument Build() => _doc;
}

public sealed class TableBuilder
{
    private readonly TableElement _table = new();

    public TableBuilder SetHeaders(params string[] headers)
    { _table.Headers = headers; return this; }

    public TableBuilder AddRow(params string[] cells)
    { _table.Rows.Add(cells); return this; }

    public TableBuilder AddRows(IEnumerable<IEnumerable<string>> rows)
    {
        foreach (var row in rows)
            _table.Rows.Add(row.ToArray());
        return this;
    }

    public TableBuilder WithStyle(Action<TableStyle> configure)
    { configure(_table.Style); return this; }

    public TableBuilder WithColumnWidths(params float[] widths)
    { _table.ColumnWidths = widths; return this; }

    public TableBuilder WithColumnAlignments(params PdfAlignment[] alignments)
    { _table.ColumnAlignments = alignments; return this; }

    public TableElement Build() => _table;
}
