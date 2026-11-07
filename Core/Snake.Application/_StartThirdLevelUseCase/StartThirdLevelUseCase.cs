using Snake.Application.Common;
using Snake.Application.Repositories;
using Snake.Domain;

namespace Snake.Application._StartThirdLevelUseCase
{
    public class StartThirdLevelUseCase
    {
        private Game _game;
        private SpawnPositionService _spawnPositionService;

        public StartThirdLevelUseCase(IGameRepository gameRepository, SpawnPositionService spawnPositionService)
        {
            ArgumentNullException.ThrowIfNull(gameRepository);
            _game = gameRepository.GetCurrentGame();
            _spawnPositionService = spawnPositionService ?? throw new ArgumentNullException(nameof(spawnPositionService));
        }

        public void StartThirdLevel()
        {
            try
            {
                if (_game.Phase != GamePhase.Ready
                    || _game.Level.Number != LevelNumber.Third)
                {
                    return;
                }

                _game.ResetObjects();
                SpawnObjects();
                _game.DoorTickCount = 0;
                if (!_game.StartLevel())
                {
                    throw new InvalidOperationException("第三關目前無法開始。");
                }
            }
            catch (Exception exception)
            {
                _game.ResetObjects();
                throw new InvalidOperationException("StartThirdLevel 執行失敗。", exception);
            }
        }

        private void SpawnObjects()
        {
            _game.Food.SpawnOrdinary(SelectPosition("普通食物"));
            _game.Food.SpawnSpecial(SelectPosition("特殊食物"));
            _game.Shield.Spawn(SelectPosition("護盾"));
            _game.Fire.Spawn(SelectPosition("火焰"));
            _game.Bomb.Spawn(SelectPosition("炸彈"));
        }

        private GridPosition SelectPosition(string objectName)
        {
            return _spawnPositionService.Select(_game)
                ?? throw new InvalidOperationException($"沒有合法格位產生第三關{objectName}。");
        }
    }
}

