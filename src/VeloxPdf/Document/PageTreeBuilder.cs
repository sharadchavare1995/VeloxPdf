namespace VeloxPdf.Document;
using VeloxPdf.Primitives;

/// <summary>
/// Builds a balanced page tree for large PDF documents.
/// For 40,000 pages uses a 3-level tree to satisfy PDF spec guidance
/// and enable fast page seeking by PDF viewers.
/// </summary>
public static class PageTreeBuilder
{
    private const int MaxKidsPerNode = 10;

    /// <summary>
    /// Builds a balanced page tree. Returns all tree node PdfDictionaries.
    /// The first item is the root node. PageObjectNumbers must already be allocated.
    /// </summary>
    public static List<PdfDictionary> BuildTree(
        List<int> pageObjectNumbers,
        Func<int> allocateObjectNumber)
    {
        var nodes = new List<PdfDictionary>();

        if (pageObjectNumbers.Count == 0) return nodes;

        // Create leaf-level page references (actual page objects, not new nodes)
        var currentLevel = pageObjectNumbers
            .Select(n => (PdfObject)new PdfReference(n))
            .ToList();

        // Bottom-up tree construction: group MaxKidsPerNode items at each level
        while (currentLevel.Count > MaxKidsPerNode)
        {
            var nextLevel = new List<PdfObject>();
            for (int i = 0; i < currentLevel.Count; i += MaxKidsPerNode)
            {
                var chunk = currentLevel.Skip(i).Take(MaxKidsPerNode).ToList();
                int nodeNum = allocateObjectNumber();
                var node = new PdfDictionary();
                node.ObjectNumber = nodeNum;
                node.Set("Type", "Pages");
                node.Set("Count", chunk.Count);
                var kids = new PdfArray();
                foreach (var c in chunk) kids.Add(c);
                node.Set("Kids", kids);
                nodes.Add(node);
                nextLevel.Add(new PdfReference(nodeNum));
            }
            currentLevel = nextLevel;
        }

        // Create root node containing whatever is left at the top level
        int rootObjNum = allocateObjectNumber();
        var root = new PdfDictionary();
        root.ObjectNumber = rootObjNum;
        root.Set("Type", "Pages");
        root.Set("Count", pageObjectNumbers.Count);
        var rootKids = new PdfArray();
        foreach (var obj in currentLevel) rootKids.Add(obj);
        root.Set("Kids", rootKids);

        // Root goes first
        nodes.Insert(0, root);

        return nodes;
    }

    /// <summary>Returns the object number of the root node (first element).</summary>
    public static int GetRootObjectNumber(List<PdfDictionary> nodes)
        => nodes.Count > 0 ? nodes[0].ObjectNumber : 0;
}
