using Snake.UseCases;

namespace Snake.Test;

public class NoSpawnPositionSelector : ISpawnPositionSelector
{
    private int _successfulSelections;
    private int _selectionCount;

    public NoSpawnPositionSelector(int successfulSelections = 0)
    {
        _successfulSelections = successfulSelections;
        _selectionCount = 0;
    }

    public GridPositionDTO? Select(List<GridPositionDTO> legalPositions)
    {
        ArgumentNullException.ThrowIfNull(legalPositions);
        if (_selectionCount < _successfulSelections && legalPositions.Count > 0)
        {
            _selectionCount++;
            return legalPositions[0];
        }

        return null;
    }
}
