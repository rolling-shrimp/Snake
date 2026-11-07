using Snake.Application.Common;
using Snake.Application.Repositories;
using Snake.Domain;

namespace Snake.Application._StartFirstLevelUseCase
{
    public class StartFirstLevelUseCase
    {
        private Game _game;
        private SpawnPositionService _spawnPositionService;

        public StartFirstLevelUseCase(IGameRepository gameRepository, SpawnPositionService spawnPositionService)
        {
            ArgumentNullException.ThrowIfNull(gameRepository);
            _game = gameRepository.GetCurrentGame();
            _spawnPositionService = spawnPositionService ?? throw new ArgumentNullException(nameof(spawnPositionService));
        }

        public void StartFirstLevel()
        {
            try
            {
                if (_game.Phase != GamePhase.Ready
                    || _game.Level.Number != LevelNumber.First)
                {
                    return;
                }

                _game.ResetObjects();
                GridPosition position = _spawnPositionService.Select(_game)
                    ?? throw new InvalidOperationException("沒有合法格位產生第一關普通食物。");
                _game.Food.SpawnOrdinary(position);
                if (!_game.StartLevel())
                {
                    throw new InvalidOperationException("第一關目前無法開始。");
                }
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException("StartFirstLevel 執行失敗。", exception);
            }
        }
    }
}

