namespace Snake.WinForms;

public class GameResultCardViewModel
{
    public GameResultCardViewModel(
        string title,
        string message,
        int? levelNumber,
        int finalLength,
        string? detail)
    {
        Title = title;
        Message = message;
        LevelNumber = levelNumber;
        FinalLength = finalLength;
        Detail = detail;
    }

    public string Title { get; }
    public string Message { get; }
    public int? LevelNumber { get; }
    public int FinalLength { get; }
    public string? Detail { get; }
}
