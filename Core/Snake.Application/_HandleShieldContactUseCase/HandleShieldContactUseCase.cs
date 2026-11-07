using Snake.Application.Repositories;
using Snake.Domain;

namespace Snake.Application._HandleShieldContactUseCase
{
    public class HandleShieldContactUseCase
    {
        private Game _game;

        public HandleShieldContactUseCase(IGameRepository gameRepository)
        {
            ArgumentNullException.ThrowIfNull(gameRepository);
            _game = gameRepository.GetCurrentGame();
        }

        public void HandleShieldContact()
        {
            try
            {
                if (!_game.HasMovedThisTick || _game.Shield.ActiveItem is null)
                {
                    return;
                }

                GridPosition head = _game.Snake.BodyCells[0];
                ShieldItem shield = _game.Shield.ActiveItem;
                if (!shield.Position.Equals(head))
                {
                    return;
                }

                _game.Shield.Collect();
                _game.Shield.MarkPending();
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException("HandleShieldContact 執行失敗。", exception);
            }
        }
    }
}

