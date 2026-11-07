using Snake.Application._ReplenishTriggeredObjectUseCase.Port;

namespace Snake.WinForms
{
    public class RandomSpawnPositionSelector : ISpawnPositionSelector
    {
        private Random _random;

        public RandomSpawnPositionSelector()
        {
            _random = new Random();
        }

        public int SelectIndex(int positionCount)
        {
            if (positionCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(positionCount));
            }

            return _random.Next(positionCount);
        }
    }
}
