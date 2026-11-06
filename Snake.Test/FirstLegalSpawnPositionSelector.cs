using Snake.UseCases;

namespace Snake.Test;

public class FirstLegalSpawnPositionSelector : ISpawnPositionSelector
{
    public GridPositionDTO? Select(List<GridPositionDTO> legalPositions)
    {
        ArgumentNullException.ThrowIfNull(legalPositions);
        return legalPositions.FirstOrDefault();
    }
}
