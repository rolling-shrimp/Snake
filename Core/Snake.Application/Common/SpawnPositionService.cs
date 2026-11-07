using Snake.Application._ReplenishTriggeredObjectUseCase.Port;
using Snake.Domain;

namespace Snake.Application.Common
{
    public class SpawnPositionService
    {
        private ISpawnPositionSelector _spawnPositionSelector;

        public SpawnPositionService(ISpawnPositionSelector spawnPositionSelector)
        {
            _spawnPositionSelector = spawnPositionSelector
                ?? throw new ArgumentNullException(nameof(spawnPositionSelector));
        }

        internal GridPosition? Select(Game game)
        {
            ArgumentNullException.ThrowIfNull(game);
            List<GridPosition> legalPositions = game.Board.GetLegalSpawnCells(
                game.Snake.BodyCells,
                game.GetSpawnBlockedCells());
            if (legalPositions.Count == 0)
            {
                return null;
            }

            int selectedIndex = _spawnPositionSelector.SelectIndex(legalPositions.Count);
            if (selectedIndex < 0 || selectedIndex >= legalPositions.Count)
            {
                throw new InvalidOperationException("產生位置選擇器回傳了超出範圍的索引。");
            }

            return legalPositions[selectedIndex];
        }
    }
}

