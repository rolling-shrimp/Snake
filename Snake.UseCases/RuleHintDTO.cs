namespace Snake.UseCases;

public class RuleHintDTO
{
    public RuleHintDTO(string code, string text)
    {
        Code = code;
        Text = text;
    }

    public string Code { get; }
    public string Text { get; }
}
