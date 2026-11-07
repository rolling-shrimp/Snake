using Snake.Application.Common;
using Snake.Application.Repositories;
using Snake.Domain;

namespace Snake.Application._RestartGameUseCase
{
    public class RestartGameUseCase
    {
        private Game _game;
        private SpawnPositionService _spawnPositionService;

        public RestartGameUseCase(IGameRepository gameRepository, SpawnPositionService spawnPositionService)
        {
            ArgumentNullException.ThrowIfNull(gameRepository);
            _game = gameRepository.GetCurrentGame();
            _spawnPositionService = spawnPositionService ?? throw new ArgumentNullException(nameof(spawnPositionService));
        }

        public void RestartGame()
        {
            try
            {
                if (_game.Phase != GamePhase.GameOver
                    && _game.Phase != GamePhase.Completed)
                {
                    return;
                }

                _game.Level.Reset();
                _game.ResetObjects();
                _game.DoorAndMaze.Reset();
                _game.GameResult.Reset();
                _game.Snake.ResetSafely(
                    _game.Board.GetFirstLevelSafeStart(),
                    _game.Board,
                    BoundaryPolicy.Wrap);
                GridPosition position = _spawnPositionService.Select(_game)
                    ?? throw new InvalidOperationException("沒有合法格位產生第一關普通食物。");
                _game.Food.SpawnOrdinary(position);
                _game.RestartPlaying();
                _game.ResetTransientState();
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException("RestartGame 執行失敗。", exception);
            }
        }
    }
}

