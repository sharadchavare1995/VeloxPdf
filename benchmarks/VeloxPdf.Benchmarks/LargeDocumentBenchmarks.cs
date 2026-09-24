namespace VeloxPdf.Benchmarks;
using BenchmarkDotNet.Attributes;
using VeloxPdf.Compression;
using VeloxPdf.IO;

[MemoryDiagnoser]
public class LargeDocumentBenchmarks
{
    private byte[] _largeData = null!;

    [GlobalSetup]
    public void Setup()
    {
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < 1000; i++)
            sb.AppendLine($"BT /F1 10 Tf 36 {800 - i % 50 * 14} Td (Row {i}: Sample data content) Tj ET");
        _largeData = System.Text.Encoding.GetEncoding("iso-8859-1").GetBytes(sb.ToString());
    }

    [Benchmark]
    public byte[] Compress_1MB_Stream()
    {
        var compressor = new FlateCompressor();
        return compressor.Compress(_largeData);
    }

    [Benchmark]
    public byte[] NoCompress_Passthrough()
    {
        var compressor = new NoCompressor();
        return compressor.Compress(_largeData);
    }

    [Benchmark]
    public void PooledMemoryStream_WriteAndRead()
    {
        using var ms = new PooledMemoryStream(64 * 1024);
        byte[] buf = new byte[1024];
        for (int i = 0; i < 100; i++)
            ms.Write(buf, 0, buf.Length);
        var _ = ms.ToArray();
    }
}
