using Snake.Application.Repositories;
using Snake.Domain;

namespace Snake.Application._RescueWithShieldUseCase
{
    public class RescueWithShieldUseCase
    {
        private Game _game;

        public RescueWithShieldUseCase(IGameRepository gameRepository)
        {
            ArgumentNullException.ThrowIfNull(gameRepository);
            _game = gameRepository.GetCurrentGame();
        }

        public void RescueWithShield()
        {
            try
            {
                if (_game.PendingDeathCause is null || !_game.Shield.CanRescue)
                {
                    return;
                }

                List<GridPosition> blockedCells = _game.GetMovementBlockedCells();
                List<GridPosition>? safeBody = _game.Board.FindSafeBody(
                    _game.PreHitLength,
                    _game.Level.BoundaryPolicy,
                    blockedCells);
                if (safeBody is null)
                {
                    throw new InvalidOperationException("護盾保命時找不到合法安全蛇身。");
                }

                if (!_game.Shield.UseCharge())
                {
                    throw new InvalidOperationException("護盾保命次數在使用前已失效。");
                }

                _game.Snake.ResetSafely(safeBody, _game.Board, _game.Level.BoundaryPolicy);
                _game.ClearPendingDeath();
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException("RescueWithShield 執行失敗。", exception);
            }
        }
    }
}

