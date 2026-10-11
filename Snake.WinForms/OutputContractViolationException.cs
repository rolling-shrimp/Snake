namespace Snake;

public class OutputContractViolationException : Exception
{
    public OutputContractViolationException(string contractMethod, string guarantee, string actualResult)
        : base($"{contractMethod}: {guarantee}; 實際結果：{actualResult}")
    {
    }
}
