namespace PatternNetwork.Gates;

public interface IGateChecker
{
    string Id { get; }

    GateCheckResult Check(SpecificationInventory inventory);
}
