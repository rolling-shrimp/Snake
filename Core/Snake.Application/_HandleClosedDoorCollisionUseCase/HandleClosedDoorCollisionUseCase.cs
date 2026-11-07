using Snake.Application.Repositories;
using Snake.Domain;

namespace Snake.Application._HandleClosedDoorCollisionUseCase
{
    public class HandleClosedDoorCollisionUseCase
    {
        private Game _game;

        public HandleClosedDoorCollisionUseCase(IGameRepository gameRepository)
        {
            ArgumentNullException.ThrowIfNull(gameRepository);
            _game = gameRepository.GetCurrentGame();
        }

        public void HandleClosedDoorCollision()
        {
            try
            {
                _game.PrepareMovement();
                if (_game.NextHead is not null
                    && _game.DoorAndMaze.Doors.Any(door =>
                        door.Position.Equals(_game.NextHead)
                        && door.Phase == DoorPhase.Closed))
                {
                    _game.MarkDeath(DeathCause.ClosedDoorCollision, _game.Snake.Length);
                }
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException("HandleClosedDoorCollision 執行失敗。", exception);
            }
        }
    }
}

