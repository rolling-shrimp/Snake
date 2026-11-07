using Snake.Application.Common;
using Snake.Application.Repositories;
using Snake.Domain;

namespace Snake.Application._ReplenishTriggeredObjectUseCase
{
    public class ReplenishTriggeredObjectUseCase
    {
        private Game _game;
        private SpawnPositionService _spawnPositionService;

        public ReplenishTriggeredObjectUseCase(IGameRepository gameRepository, SpawnPositionService spawnPositionService)
        {
            ArgumentNullException.ThrowIfNull(gameRepository);
            _game = gameRepository.GetCurrentGame();
            _spawnPositionService = spawnPositionService ?? throw new ArgumentNullException(nameof(spawnPositionService));
        }

        public void ReplenishTriggeredObject()
        {
            try
            {
                if (_game.Phase != GamePhase.Playing)
                {
                    return;
                }

                ReplenishFood();
                ReplenishShield();
                ReplenishFire();
                ReplenishBomb();
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException("ReplenishTriggeredObject 執行失敗。", exception);
            }
        }

        private void ReplenishFood()
        {
            foreach (FoodType foodType in _game.Food.PendingTypes)
            {
                GridPosition? position = _spawnPositionService.Select(_game);
                if (position is null)
                {
                    continue;
                }

                if (foodType == FoodType.Ordinary)
                {
                    _game.Food.SpawnOrdinary(position);
                }
                else
                {
                    _game.Food.SpawnSpecial(position);
                }

                _game.Food.RemovePending(foodType);
            }
        }

        private void ReplenishShield()
        {
            if (!_game.Shield.PendingSpawn)
            {
                return;
            }

            GridPosition? position = _spawnPositionService.Select(_game);
            if (position is not null)
            {
                _game.Shield.Spawn(position);
            }
        }

        private void ReplenishFire()
        {
            if (!_game.Fire.PendingSpawn)
            {
                return;
            }

            GridPosition? position = _spawnPositionService.Select(_game);
            if (position is not null)
            {
                _game.Fire.Spawn(position);
            }
        }

        private void ReplenishBomb()
        {
            if (!_game.Bomb.PendingSpawn)
            {
                return;
            }

            GridPosition? position = _spawnPositionService.Select(_game);
            if (position is not null)
            {
                _game.Bomb.Spawn(position);
            }
        }
    }
}

