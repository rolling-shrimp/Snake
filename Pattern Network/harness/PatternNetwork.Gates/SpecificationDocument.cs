namespace PatternNetwork.Gates;

public class SpecificationDocument
{
    public SpecificationDocument(
        string filePath,
        SpecificationDocumentHeader header,
        List<SpecificationReference> references)
    {
        FilePath = filePath;
        Header = header;
        References = references;
    }

    public string FilePath { get; set; }

    public SpecificationDocumentHeader Header { get; set; }

    public List<SpecificationReference> References { get; set; }
}
