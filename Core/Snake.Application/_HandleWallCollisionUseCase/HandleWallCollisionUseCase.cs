using Snake.Application.Repositories;
using Snake.Domain;

namespace Snake.Application._HandleWallCollisionUseCase
{
    public class HandleWallCollisionUseCase
    {
        private Game _game;

        public HandleWallCollisionUseCase(IGameRepository gameRepository)
        {
            ArgumentNullException.ThrowIfNull(gameRepository);
            _game = gameRepository.GetCurrentGame();
        }

        public void HandleWallCollision()
        {
            try
            {
                _game.PrepareMovement();
                if (_game.NextHead is not null
                    && _game.DoorAndMaze.WallCells.Contains(_game.NextHead))
                {
                    _game.MarkDeath(DeathCause.WallCollision, _game.Snake.Length);
                }
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException("HandleWallCollision 執行失敗。", exception);
            }
        }
    }
}

