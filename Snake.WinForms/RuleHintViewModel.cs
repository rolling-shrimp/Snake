namespace Snake.WinForms;

public class RuleHintViewModel
{
    public RuleHintViewModel(string code, string text)
    {
        Code = code;
        Text = text;
    }

    public string Code { get; }
    public string Text { get; }
}
