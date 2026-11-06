namespace Snake.UseCases;

public interface ISpawnPositionSelector
{
    GridPositionDTO? Select(List<GridPositionDTO> legalPositions);
}
