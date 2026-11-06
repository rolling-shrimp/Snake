namespace PatternNetwork.Gates;

public class GateCheckerRegistry
{
    private Dictionary<string, Func<SpecificationInventory, GateCheckResult>> _checkers;
    private List<string> _registrationOrder;

    public GateCheckerRegistry()
    {
        _checkers = new Dictionary<string, Func<SpecificationInventory, GateCheckResult>>(StringComparer.Ordinal);
        _registrationOrder = new List<string>();
    }

    public void Register(string checkerId, Func<SpecificationInventory, GateCheckResult> checker)
    {
        if (string.IsNullOrWhiteSpace(checkerId))
        {
            throw new ArgumentException("checker ID 不得為空。", nameof(checkerId));
        }

        ArgumentNullException.ThrowIfNull(checker);
        if (!_checkers.TryAdd(checkerId, checker))
        {
            throw new ArgumentException($"checker ID 不得重複：{checkerId}。", nameof(checkerId));
        }

        _registrationOrder.Add(checkerId);
    }

    public void Register(IGateChecker checker)
    {
        ArgumentNullException.ThrowIfNull(checker);
        Register(checker.Id, checker.Check);
    }

    public List<GateCheckResult> RunAll(SpecificationInventory inventory)
    {
        ArgumentNullException.ThrowIfNull(inventory);
        List<GateCheckResult> results = new List<GateCheckResult>();
        foreach (string checkerId in _registrationOrder)
        {
            results.Add(_checkers[checkerId](inventory));
        }

        return results;
    }

    public bool TryRun(
        string checkerId,
        SpecificationInventory inventory,
        out GateCheckResult? result)
    {
        if (string.IsNullOrWhiteSpace(checkerId))
        {
            throw new ArgumentException("checker ID 不得為空。", nameof(checkerId));
        }

        ArgumentNullException.ThrowIfNull(inventory);
        if (!_checkers.TryGetValue(checkerId, out Func<SpecificationInventory, GateCheckResult>? checker))
        {
            result = null;
            return false;
        }

        result = checker(inventory);
        return true;
    }
}
