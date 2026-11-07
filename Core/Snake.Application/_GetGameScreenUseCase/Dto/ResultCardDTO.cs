namespace Snake.Application._GetGameScreenUseCase.Dto
{
    public class ResultCardDTO
    {
        public ResultCardDTO(string outcome, int levelNumber, int finalLength, string? deathCause)
        {
            if (string.IsNullOrWhiteSpace(outcome))
            {
                throw new ArgumentException("結果不得為空。", nameof(outcome));
            }

            Outcome = outcome;
            LevelNumber = levelNumber;
            FinalLength = finalLength;
            DeathCause = deathCause;
        }

        public string Outcome { get; }
        public int LevelNumber { get; }
        public int FinalLength { get; }
        public string? DeathCause { get; }
    }
}
