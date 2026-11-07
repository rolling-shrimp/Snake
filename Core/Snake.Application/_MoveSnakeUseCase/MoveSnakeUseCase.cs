using Snake.Application.Repositories;
using Snake.Domain;

namespace Snake.Application._MoveSnakeUseCase
{
    public class MoveSnakeUseCase
    {
        private Game _game;

        public MoveSnakeUseCase(IGameRepository gameRepository)
        {
            ArgumentNullException.ThrowIfNull(gameRepository);
            _game = gameRepository.GetCurrentGame();
        }

        public void MoveSnake()
        {
            try
            {
                _game.PrepareMovement();
                if (_game.MovementPlan is null || _game.PendingDeathCause is not null)
                {
                    return;
                }

                _game.Snake.ReplaceBodyCells(
                    _game.MovementPlan,
                    _game.Board,
                    _game.Level.BoundaryPolicy);
                _game.MarkMovementCommitted();
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException("MoveSnake 執行失敗。", exception);
            }
        }
    }
}

