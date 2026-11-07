using Snake.Application.Repositories;
using Snake.Domain;

namespace Snake.Application._HandleSelfCollisionUseCase
{
    public class HandleSelfCollisionUseCase
    {
        private Game _game;

        public HandleSelfCollisionUseCase(IGameRepository gameRepository)
        {
            ArgumentNullException.ThrowIfNull(gameRepository);
            _game = gameRepository.GetCurrentGame();
        }

        public void HandleSelfCollision()
        {
            try
            {
                _game.PrepareMovement();
                if (_game.NextHead is not null
                    && _game.MovementPlan is not null
                    && _game.MovementPlan.SelfCollisionCells.Contains(_game.NextHead))
                {
                    _game.MarkDeath(DeathCause.SelfCollision, _game.Snake.Length);
                }
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException("HandleSelfCollision 執行失敗。", exception);
            }
        }
    }
}

