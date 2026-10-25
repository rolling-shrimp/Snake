namespace Snake.UseCases;

public class LevelPassedResultRspDTO
{
    public LevelPassedResultRspDTO(
        int completedLevel,
        int finalLength,
        string bannerText,
        bool isScoreCardVisible)
    {
        CompletedLevel = completedLevel;
        FinalLength = finalLength;
        BannerText = bannerText;
        IsScoreCardVisible = isScoreCardVisible;
    }

    public int CompletedLevel { get; }
    public int FinalLength { get; }
    public string BannerText { get; }
    public bool IsScoreCardVisible { get; }
}
