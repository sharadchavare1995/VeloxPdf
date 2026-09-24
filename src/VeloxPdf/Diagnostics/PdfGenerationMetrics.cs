namespace VeloxPdf.Diagnostics;

public sealed record PdfGenerationMetrics(
    string TemplateName,
    int PageCount,
    long FileSizeBytes,
    TimeSpan Duration,
    DateTime Timestamp,
    bool Success,
    string? ErrorMessage)
{
    public static PdfGenerationMetrics ForSuccess(
        string templateName, int pageCount, long fileSize, TimeSpan duration)
        => new(templateName, pageCount, fileSize,
               duration, DateTime.UtcNow, true, null);

    public static PdfGenerationMetrics ForFailure(
        string templateName, TimeSpan duration, string errorMessage)
        => new(templateName, 0, 0,
               duration, DateTime.UtcNow, false, errorMessage);

    public double PagesPerSecond => Duration.TotalSeconds > 0
        ? PageCount / Duration.TotalSeconds : 0;

    public double MegabytesPerSecond => Duration.TotalSeconds > 0
        ? FileSizeBytes / 1024.0 / 1024.0 / Duration.TotalSeconds : 0;

    public override string ToString() =>
        $"[{(Success ? "OK" : "FAIL")}] {TemplateName}: " +
        $"{PageCount} pages, {FileSizeBytes / 1024}KB, " +
        $"{Duration.TotalMilliseconds:F0}ms";
}
