using Snake.UseCases;

namespace Snake.WinForms;

public class RandomSpawnPositionSelector : ISpawnPositionSelector
{
    private Random _random;

    public RandomSpawnPositionSelector()
    {
        _random = new Random();
    }

    public GridPositionDTO? Select(List<GridPositionDTO> legalPositions)
    {
        ArgumentNullException.ThrowIfNull(legalPositions);
        if (legalPositions.Count == 0)
        {
            return null;
        }

        return legalPositions[_random.Next(legalPositions.Count)];
    }
}
