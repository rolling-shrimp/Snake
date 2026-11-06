namespace PatternNetwork.Gates;

public class SpecificationInventory
{
    public SpecificationInventory(List<SpecificationDocument> documents)
    {
        Documents = documents;
    }

    public List<SpecificationDocument> Documents { get; set; }
}
