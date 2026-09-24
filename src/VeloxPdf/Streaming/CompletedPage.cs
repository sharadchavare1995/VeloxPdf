namespace VeloxPdf.Streaming;

public sealed class CompletedPage
{
    public int PageNumber { get; init; }
    public int PageObjectNumber { get; init; }
    public int ContentObjectNumber { get; init; }
    public byte[] CompressedContent { get; init; } = Array.Empty<byte>();
    public int ContentLength { get; init; }
    public long XrefOffset { get; set; }
}
