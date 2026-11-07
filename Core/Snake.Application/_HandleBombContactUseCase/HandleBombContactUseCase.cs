using Snake.Application.Repositories;
using Snake.Domain;

namespace Snake.Application._HandleBombContactUseCase
{
    public class HandleBombContactUseCase
    {
        private Game _game;

        public HandleBombContactUseCase(IGameRepository gameRepository)
        {
            ArgumentNullException.ThrowIfNull(gameRepository);
            _game = gameRepository.GetCurrentGame();
        }

        public void HandleBombContact()
        {
            try
            {
                if (!_game.HasMovedThisTick || _game.Bomb.ActiveItem is null)
                {
                    return;
                }

                GridPosition head = _game.Snake.BodyCells[0];
                BombItem bomb = _game.Bomb.ActiveItem;
                if (!bomb.Position.Equals(head))
                {
                    return;
                }

                int preHitLength = _game.Snake.Length;
                _game.Bomb.Consume();
                _game.Bomb.MarkPending();
                _game.MarkDeath(DeathCause.Bomb, preHitLength);
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException("HandleBombContact 執行失敗。", exception);
            }
        }
    }
}

