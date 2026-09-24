using System.Diagnostics;
using VeloxPdf.Samples;

Console.OutputEncoding = System.Text.Encoding.UTF8;

Console.WriteLine("╔══════════════════════════════════════════════════════╗");
Console.WriteLine("║           VeloxPdf -- PDF Generation Library         ║");
Console.WriteLine("║         Zero Dependencies  ·  High Performance       ║");
Console.WriteLine("╚══════════════════════════════════════════════════════╝");
Console.WriteLine();

string outputDir = Path.Combine(Directory.GetCurrentDirectory(), "output");
Directory.CreateDirectory(outputDir);
Console.WriteLine($"Output directory: {outputDir}");
Console.WriteLine();

while (true)
{
    Console.WriteLine("Select a sample to run:");
    Console.WriteLine("  1  Invoice PDF (professional invoice with line items)");
    Console.WriteLine("  2  Business Report PDF (~11 pages, 10 sections)");
    Console.WriteLine("  3  Formal Business Letter");
    Console.WriteLine("  4  All three in parallel (thread-safety demo)");
    Console.WriteLine("  5  Large Scale: 10,000 records");
    Console.WriteLine("  6  Large Scale: 100,000 records");
    Console.WriteLine("  7  Large Scale: 1,000,000 records");
    Console.WriteLine("  Q  Quit");
    Console.Write("\nChoice: ");

    string? choice = Console.ReadLine()?.Trim().ToUpperInvariant();

    if (choice is "Q" or "QUIT" or "EXIT") break;

    var sw = Stopwatch.StartNew();

    try
    {
        switch (choice)
        {
            case "1":
            {
                Console.WriteLine("\nGenerating invoice...");
                byte[] pdf = InvoiceSample.Generate();
                string path = Path.Combine(outputDir, "invoice.pdf");
                await File.WriteAllBytesAsync(path, pdf);
                sw.Stop();
                PrintResult("Invoice", path, pdf.Length, sw.Elapsed);
                break;
            }

            case "2":
            {
                Console.WriteLine("\nGenerating report...");
                byte[] pdf = ReportSample.Generate();
                string path = Path.Combine(outputDir, "report.pdf");
                await File.WriteAllBytesAsync(path, pdf);
                sw.Stop();
                PrintResult("Report", path, pdf.Length, sw.Elapsed);
                break;
            }

            case "3":
            {
                Console.WriteLine("\nGenerating letter...");
                var (_, _, letter) = await MultiTemplateSample.GenerateAllAsync();
                string path = Path.Combine(outputDir, "letter.pdf");
                await File.WriteAllBytesAsync(path, letter);
                sw.Stop();
                PrintResult("Letter", path, letter.Length, sw.Elapsed);
                break;
            }

            case "4":
            {
                Console.WriteLine("\nGenerating all three in parallel...");
                var (invoice, report, letter) = await MultiTemplateSample.GenerateAllAsync();
                string invPath = Path.Combine(outputDir, "invoice_parallel.pdf");
                string repPath = Path.Combine(outputDir, "report_parallel.pdf");
                string letPath = Path.Combine(outputDir, "letter_parallel.pdf");
                await File.WriteAllBytesAsync(invPath, invoice);
                await File.WriteAllBytesAsync(repPath, report);
                await File.WriteAllBytesAsync(letPath, letter);
                sw.Stop();
                Console.WriteLine($"\nGenerated 3 PDFs in {sw.Elapsed.TotalMilliseconds:F0}ms");
                Console.WriteLine($"  Invoice:  {invoice.Length / 1024}KB -> {invPath}");
                Console.WriteLine($"  Report:   {report.Length / 1024}KB -> {repPath}");
                Console.WriteLine($"  Letter:   {letter.Length / 1024}KB -> {letPath}");
                break;
            }

            case "5":
            {
                string path = Path.Combine(outputDir, "scale_10k.pdf");
                await LargeScaleSample.Generate(10_000, path);
                break;
            }

            case "6":
            {
                string path = Path.Combine(outputDir, "scale_100k.pdf");
                await LargeScaleSample.Generate(100_000, path);
                break;
            }

            case "7":
            {
                string path = Path.Combine(outputDir, "scale_1m.pdf");
                await LargeScaleSample.Generate(1_000_000, path);
                break;
            }

            default:
                Console.WriteLine("Invalid choice. Please enter 1-7 or Q.");
                break;
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"\nError: {ex.GetType().Name}: {ex.Message}");
        if (ex.InnerException != null)
            Console.WriteLine($"  Inner: {ex.InnerException.Message}");
    }

    Console.WriteLine();
}

Console.WriteLine("Goodbye!");

static void PrintResult(string name, string path, long sizeBytes, TimeSpan elapsed)
{
    Console.WriteLine($"\n{name} generated successfully!");
    Console.WriteLine($"  File: {path}");
    Console.WriteLine($"  Size: {sizeBytes / 1024}KB ({sizeBytes:N0} bytes)");
    Console.WriteLine($"  Time: {elapsed.TotalMilliseconds:F0}ms");
}
