namespace VeloxPdf.Document;
using VeloxPdf.Compression;
using VeloxPdf.Fonts;
using VeloxPdf.Images;
using VeloxPdf.IO;
using VeloxPdf.Layout;
using VeloxPdf.Primitives;
using VeloxPdf.Elements;

public sealed class PdfDocument
{
    public PdfMetadata Metadata { get; set; } = new();
    public PdfPageSize DefaultPageSize { get; set; } = PdfPageSize.A4;
    public PdfMargins DefaultMargins { get; set; } = PdfMargins.Default;
    public PdfFont DefaultFont { get; set; } = Type1Font.Helvetica;
    public float DefaultFontSize { get; set; } = 11f;
    public List<PdfPage> Pages { get; } = new();
    public List<IPdfElement> Elements { get; } = new();

    private readonly List<PdfFont> _globalFonts = new();
    private readonly List<PdfImage> _globalImages = new();

    public PdfPage AddPage()
        => AddPage(DefaultPageSize, DefaultMargins);

    public PdfPage AddPage(PdfPageSize size, PdfMargins? margins = null)
    {
        var page = new PdfPage(size.Width, size.Height, margins ?? DefaultMargins);
        Pages.Add(page);
        return page;
    }

    public void RegisterFont(PdfFont font) => _globalFonts.Add(font);
    public void RegisterImage(PdfImage image) => _globalImages.Add(image);

    private void RenderElements()
    {
        if (Elements.Count == 0) return;

        var page = AddPage();
        var layout = new LayoutEngine(page.Width, page.Height, page.Margins);
        var ctx = new RenderContext(this, page, layout, DefaultFont, DefaultFontSize);

        foreach (var element in Elements)
        {
            element.Render(ctx);
        }
    }

    public byte[] Save()
    {
        using var ms = new MemoryStream();
        Save(ms);
        return ms.ToArray();
    }

    public void Save(Stream output)
    {
        // Render elements if we have any and no explicit pages added
        if (Elements.Count > 0 && Pages.Count == 0)
            RenderElements();

        if (Pages.Count == 0)
            AddPage(); // Ensure at least one page

        // Reuse the static singleton — avoids a heap allocation per Save() call.
        // Use Fastest compression for large page counts: financial-report content
        // streams are short text, so Optimal vs Fastest yields negligible size
        // difference while Optimal uses more CPU and intermediate buffer memory.
        var compressor = Pages.Count > 200 ? FlateCompressor.Fastest : FlateCompressor.Default;

        // Capture once — avoids repeated property reads through the List wrapper
        // and lets the compiler confirm pageCount is stable across all steps.
        int pageCount = Pages.Count;

        // Pre-allocate the xref list to the expected object count so the internal
        // array never doubles past the 85 KB LOH threshold during Save().
        // Without this hint, a 20,000-page document causes 5+ LOH array doublings
        // (~786 KB total) that fragment the Large Object Heap and cause OOM on
        // subsequent runs even when total memory is well below the container limit.
        int expectedObjects = pageCount * 2 + 8; // 2 per page + catalog/info/page-tree/fonts
        using var writer = new PdfWriter(output, expectedObjects);

        // Step 1: Write header
        writer.WriteHeader();

        // Rent object-number arrays from ArrayPool so their backing int[] never
        // allocates on the GC heap. At 20,000 pages, new int[20000] = 80 KB —
        // just under the 85 KB LOH threshold; larger documents cross it and fragment
        // LOH cumulatively across report runs. Pooled arrays avoid that entirely.
        int[] pageObjNums    = ArrayPool<int>.Shared.Rent(pageCount);
        int[] contentObjNums = ArrayPool<int>.Shared.Rent(pageCount);
        try
        {
            // Step 2: Allocate object numbers for all pages and content streams
            for (int i = 0; i < pageCount; i++)
            {
                pageObjNums[i]    = writer.AllocateObjectNumber();
                contentObjNums[i] = writer.AllocateObjectNumber();
            }

            // Allocate for page tree
            int pageTreeObjNum = writer.AllocateObjectNumber();

            // Step 3: Write content streams — Reset() after encoding so the
            // StringBuilder is freed progressively rather than held for the
            // entire Save() call (important for large page counts).
            for (int i = 0; i < pageCount; i++)
            {
                var page = Pages[i];
                byte[] contentBytes = page.ContentStream.GetBytes();
                page.ContentStream.Reset(); // free StringBuilder backing store

                var contentStream = new PdfStream();
                contentStream.ObjectNumber = contentObjNums[i];
                contentStream.SetRawData(contentBytes, compressor);
                writer.WriteObject(contentStream);
            }

            // Step 4: Write page objects
            for (int i = 0; i < pageCount; i++)
            {
                var page = Pages[i];
                var pageDict = new PdfDictionary();
                pageDict.ObjectNumber = pageObjNums[i];
                pageDict.Set("Type", "Page");
                pageDict.Set("Parent", new PdfReference(pageTreeObjNum));

                var mediaBox = new PdfArray();
                mediaBox.Add(0f);
                mediaBox.Add(0f);
                mediaBox.Add(page.Width);
                mediaBox.Add(page.Height);
                pageDict.Set("MediaBox", mediaBox);

                pageDict.Set("Contents", new PdfReference(contentObjNums[i]));

                // Build resource dictionary
                var resources = BuildResourceDict(page);
                pageDict.Set("Resources", resources);

                writer.WriteObject(pageDict);
            }

            // Step 5: Write page tree
            var pageTree = new PdfDictionary();
            pageTree.ObjectNumber = pageTreeObjNum;
            pageTree.Set("Type", "Pages");
            pageTree.Set("Count", pageCount);
            var kids = new PdfArray();
            for (int i = 0; i < pageCount; i++)
                kids.Add(new PdfReference(pageObjNums[i]));
            pageTree.Set("Kids", kids);
            writer.WriteObject(pageTree);

            // Step 6: Write info object
            int infoObjNum = writer.AllocateObjectNumber();
            var infoDict = Metadata.BuildInfoDictionary();
            infoDict.ObjectNumber = infoObjNum;
            writer.WriteObject(infoDict);

            // Step 7: Write catalog
            int catalogObjNum = writer.AllocateObjectNumber();
            var catalog = new PdfDictionary();
            catalog.ObjectNumber = catalogObjNum;
            catalog.Set("Type", "Catalog");
            catalog.Set("Pages", new PdfReference(pageTreeObjNum));
            writer.WriteObject(catalog);

            // Step 8: Write xref and trailer
            writer.WriteXRefAndTrailer(catalogObjNum, infoObjNum, catalogObjNum);
        }
        finally
        {
            // Return both arrays to the pool regardless of success or failure.
            ArrayPool<int>.Shared.Return(pageObjNums);
            ArrayPool<int>.Shared.Return(contentObjNums);
        }
    }

    private PdfDictionary BuildResourceDict(PdfPage page)
    {
        var resources = new PdfDictionary();

        // Fonts
        var fonts = page.Fonts;
        if (fonts.Count > 0)
        {
            var fontDict = new PdfDictionary();
            foreach (var (alias, font) in fonts)
            {
                fontDict.Set(alias, font.BuildFontDictionary());
            }
            resources.Set("Font", fontDict);
        }

        // XObjects (images)
        var images = page.Images;
        if (images.Count > 0)
        {
            var xobjects = new PdfDictionary();
            foreach (var (alias, image) in images)
            {
                var imgDict = image.BuildImageDictionary();
                xobjects.Set(alias, imgDict);
            }
            resources.Set("XObject", xobjects);
        }

        var procSet = new PdfArray();
        procSet.Add("PDF");
        procSet.Add("Text");
        if (images.Count > 0)
        {
            procSet.Add("ImageB");
            procSet.Add("ImageC");
        }
        resources.Set("ProcSet", procSet);

        return resources;
    }

    public async Task SaveAsync(Stream output, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        await Task.Run(() => Save(output), ct);
    }

    /// <summary>
    /// Streaming save: writes each page to <paramref name="output"/> immediately
    /// as it is produced by <paramref name="pages"/>, then discards the
    /// <see cref="PdfPage"/>.  Peak memory = one PdfPage + ContentStream at a time,
    /// regardless of the total page count.
    ///
    /// Memory comparison vs <see cref="Save(Stream)"/>:
    /// <list type="bullet">
    ///   <item>Save(Stream): all ContentStreams held simultaneously (~240 MB for 20,000 pages)</item>
    ///   <item>SavePageStream: one ContentStream at a time (~12 KB per page max)</item>
    /// </list>
    ///
    /// Use when the caller can generate pages lazily (e.g. via <c>yield return</c>).
    /// </summary>
    /// <param name="estimatedPageCount">
    /// Hint for pre-allocating the xref list and the page-object-number buffer.
    /// Pass <c>pages.Count</c> when the count is known; set to 0 or a rough guess
    /// when unknown (the buffer will grow automatically at negligible cost).
    /// </param>
    public static void SavePageStream(
        Stream output,
        PdfMetadata metadata,
        IEnumerable<PdfPage> pages,
        ICompressor? compressor = null,
        int estimatedPageCount = 64)
    {
        if (estimatedPageCount < 4) estimatedPageCount = 4;
        compressor ??= FlateCompressor.Fastest;

        // 2 objects per page + catalog / info / page-tree.
        int expectedObjects = estimatedPageCount * 2 + 8;
        using var writer = new PdfWriter(output, expectedObjects);
        writer.WriteHeader();

        // Allocate the page-tree object number BEFORE any pages so that each
        // page dictionary can reference /Parent {pageTreeObjNum} 0 R immediately.
        int pageTreeObjNum = writer.AllocateObjectNumber();

        // Track page object numbers in a pooled array so the backing int[] never
        // crosses the 85 KB LOH threshold (80 KB at 20,000 pages exactly).
        int[] pageObjNums = ArrayPool<int>.Shared.Rent(estimatedPageCount);
        int   pageCount   = 0;

        try
        {
            foreach (var page in pages)
            {
                // Grow the buffer if the estimated page count was too small.
                if (pageCount >= pageObjNums.Length)
                {
                    int   newCap   = pageObjNums.Length * 2;
                    int[] enlarged = ArrayPool<int>.Shared.Rent(newCap);
                    pageObjNums.AsSpan(0, pageCount).CopyTo(enlarged);
                    ArrayPool<int>.Shared.Return(pageObjNums);
                    pageObjNums = enlarged;
                }

                int contentObjNum            = writer.AllocateObjectNumber();
                int pageObjNum               = writer.AllocateObjectNumber();
                pageObjNums[pageCount++]     = pageObjNum;

                // Compress and write the content stream immediately.
                // ContentStream.Reset() frees the StringBuilder backing store so
                // the page's content memory is reclaimed before the next page starts.
                byte[] contentBytes = page.ContentStream.GetBytes();
                page.ContentStream.Reset();

                var contentStream = new PdfStream();
                contentStream.ObjectNumber = contentObjNum;
                contentStream.SetRawData(contentBytes, compressor);
                writer.WriteObject(contentStream);

                // Write the page dictionary.
                var mediaBox = new PdfArray();
                mediaBox.Add(0f); mediaBox.Add(0f);
                mediaBox.Add(page.Width); mediaBox.Add(page.Height);

                var pageDict = new PdfDictionary();
                pageDict.ObjectNumber = pageObjNum;
                pageDict.Set("Type",      "Page");
                pageDict.Set("Parent",    new PdfReference(pageTreeObjNum));
                pageDict.Set("MediaBox",  mediaBox);
                pageDict.Set("Contents",  new PdfReference(contentObjNum));
                pageDict.Set("Resources", page.BuildResourceDictionary());
                writer.WriteObject(pageDict);

                // pdfPage (and its ContentStream) are no longer referenced here.
                // The GC can collect them on the next gen0 sweep.
            }

            // Page tree — needs all page object numbers collected above.
            var kids = new PdfArray();
            for (int i = 0; i < pageCount; i++)
                kids.Add(new PdfReference(pageObjNums[i]));

            var pageTree = new PdfDictionary();
            pageTree.ObjectNumber = pageTreeObjNum;
            pageTree.Set("Type",  "Pages");
            pageTree.Set("Count", pageCount);
            pageTree.Set("Kids",  kids);
            writer.WriteObject(pageTree);

            // Info dictionary
            int infoObjNum = writer.AllocateObjectNumber();
            var infoDict   = metadata.BuildInfoDictionary();
            infoDict.ObjectNumber = infoObjNum;
            writer.WriteObject(infoDict);

            // Catalog
            int catalogObjNum = writer.AllocateObjectNumber();
            var catalog       = new PdfDictionary();
            catalog.ObjectNumber = catalogObjNum;
            catalog.Set("Type",  "Catalog");
            catalog.Set("Pages", new PdfReference(pageTreeObjNum));
            writer.WriteObject(catalog);

            // catalogObjNum == writer.ObjectCount because catalog is the last object.
            writer.WriteXRefAndTrailer(catalogObjNum, infoObjNum, catalogObjNum);
        }
        finally
        {
            ArrayPool<int>.Shared.Return(pageObjNums);
        }
    }
}
