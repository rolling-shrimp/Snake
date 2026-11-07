namespace Snake.Application._ReplenishTriggeredObjectUseCase.Port
{
    public interface ISpawnPositionSelector
    {
        int SelectIndex(int positionCount);
    }
}
