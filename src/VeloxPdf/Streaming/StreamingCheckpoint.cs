namespace VeloxPdf.Streaming;

public sealed class StreamingCheckpoint
{
    public long RowsProcessed { get; set; }
    public int PagesWritten { get; set; }
    public long StreamPosition { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public void Save(Stream output)
    {
        using var bw = new BinaryWriter(output, System.Text.Encoding.UTF8, leaveOpen: true);
        bw.Write(RowsProcessed);
        bw.Write(PagesWritten);
        bw.Write(StreamPosition);
        bw.Write(CreatedAt.ToBinary());
    }

    public static StreamingCheckpoint Load(Stream input)
    {
        using var br = new BinaryReader(input, System.Text.Encoding.UTF8, leaveOpen: true);
        return new StreamingCheckpoint
        {
            RowsProcessed = br.ReadInt64(),
            PagesWritten = br.ReadInt32(),
            StreamPosition = br.ReadInt64(),
            CreatedAt = DateTime.FromBinary(br.ReadInt64())
        };
    }
}
