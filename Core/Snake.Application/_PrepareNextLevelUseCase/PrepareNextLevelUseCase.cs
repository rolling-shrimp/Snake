using Snake.Application.Repositories;
using Snake.Domain;

namespace Snake.Application._PrepareNextLevelUseCase
{
    public class PrepareNextLevelUseCase
    {
        private Game _game;

        public PrepareNextLevelUseCase(IGameRepository gameRepository)
        {
            ArgumentNullException.ThrowIfNull(gameRepository);
            _game = gameRepository.GetCurrentGame();
        }

        public void PrepareNextLevel()
        {
            try
            {
                if (!_game.NextLevelPreparationPending)
                {
                    return;
                }

                if (!_game.Level.Advance())
                {
                    throw new InvalidOperationException("第三關之後沒有下一關。");
                }

                _game.ResetObjects();
                _game.DoorAndMaze.Reset();
                if (_game.Level.Number == LevelNumber.Third)
                {
                    _game.DoorAndMaze.PrepareThirdLevel();
                }

                List<GridPosition> blockedCells = _game.DoorAndMaze.GetUnavailableCells();
                List<GridPosition>? safeBody = _game.Board.FindSafeBody(
                    3,
                    _game.Level.BoundaryPolicy,
                    blockedCells);
                if (safeBody is null)
                {
                    throw new InvalidOperationException("下一關沒有合法的三格安全蛇身。");
                }

                _game.Snake.ResetSafely(safeBody, _game.Board, _game.Level.BoundaryPolicy);
                _game.NextLevelPreparationPending = false;
                _game.DoorTickCount = 0;
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException("PrepareNextLevel 執行失敗。", exception);
            }
        }
    }
}

