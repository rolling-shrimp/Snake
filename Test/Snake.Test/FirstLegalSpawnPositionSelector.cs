using Snake.Application._ReplenishTriggeredObjectUseCase.Port;

namespace Snake.Test
{
    public class FirstLegalSpawnPositionSelector : ISpawnPositionSelector
    {
        public int SelectIndex(int positionCount)
        {
            if (positionCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(positionCount));
            }

            return 0;
        }
    }
}
