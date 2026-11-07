using Snake.Application.Common;
using Snake.Application.Repositories;
using Snake.Domain;

namespace Snake.Application._StartSecondLevelUseCase
{
    public class StartSecondLevelUseCase
    {
        private Game _game;
        private SpawnPositionService _spawnPositionService;

        public StartSecondLevelUseCase(IGameRepository gameRepository, SpawnPositionService spawnPositionService)
        {
            ArgumentNullException.ThrowIfNull(gameRepository);
            _game = gameRepository.GetCurrentGame();
            _spawnPositionService = spawnPositionService ?? throw new ArgumentNullException(nameof(spawnPositionService));
        }

        public void StartSecondLevel()
        {
            try
            {
                if (_game.Phase != GamePhase.Ready
                    || _game.Level.Number != LevelNumber.Second)
                {
                    return;
                }

                _game.ResetObjects();
                SpawnOrdinaryFood();
                SpawnSpecialFood();
                SpawnShield();
                SpawnFire();
                if (!_game.StartLevel())
                {
                    throw new InvalidOperationException("第二關目前無法開始。");
                }
            }
            catch (Exception exception)
            {
                _game.ResetObjects();
                throw new InvalidOperationException("StartSecondLevel 執行失敗。", exception);
            }
        }

        private void SpawnOrdinaryFood()
        {
            GridPosition position = SelectPosition("普通食物");
            _game.Food.SpawnOrdinary(position);
        }

        private void SpawnSpecialFood()
        {
            GridPosition position = SelectPosition("特殊食物");
            _game.Food.SpawnSpecial(position);
        }

        private void SpawnShield()
        {
            GridPosition position = SelectPosition("護盾");
            _game.Shield.Spawn(position);
        }

        private void SpawnFire()
        {
            GridPosition position = SelectPosition("火焰");
            _game.Fire.Spawn(position);
        }

        private GridPosition SelectPosition(string objectName)
        {
            return _spawnPositionService.Select(_game)
                ?? throw new InvalidOperationException($"沒有合法格位產生第二關{objectName}。");
        }
    }
}

