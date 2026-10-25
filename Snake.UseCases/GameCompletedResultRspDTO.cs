namespace Snake.UseCases;

public class GameCompletedResultRspDTO
{
    public GameCompletedResultRspDTO(
        int finalLength,
        int progressPercent,
        string successMessage,
        bool isReplayButtonVisible)
    {
        FinalLength = finalLength;
        ProgressPercent = progressPercent;
        SuccessMessage = successMessage;
        IsReplayButtonVisible = isReplayButtonVisible;
    }

    public int FinalLength { get; }
    public int ProgressPercent { get; }
    public string SuccessMessage { get; }
    public bool IsReplayButtonVisible { get; }
}
