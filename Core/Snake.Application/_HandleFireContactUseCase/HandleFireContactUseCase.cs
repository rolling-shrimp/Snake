using Snake.Application.Repositories;
using Snake.Domain;

namespace Snake.Application._HandleFireContactUseCase
{
    public class HandleFireContactUseCase
    {
        private Game _game;

        public HandleFireContactUseCase(IGameRepository gameRepository)
        {
            ArgumentNullException.ThrowIfNull(gameRepository);
            _game = gameRepository.GetCurrentGame();
        }

        public void HandleFireContact()
        {
            try
            {
                if (!_game.HasMovedThisTick || _game.Fire.ActiveItem is null)
                {
                    return;
                }

                GridPosition head = _game.Snake.BodyCells[0];
                FireItem fire = _game.Fire.ActiveItem;
                if (!fire.Position.Equals(head))
                {
                    return;
                }

                int preHitLength = _game.Snake.Length;
                _game.Fire.Consume();
                _game.Fire.MarkPending();
                LengthChangeResult lengthChange = _game.Snake.CalculateLengthChange(_game.Fire.LengthDelta);
                if (lengthChange.IsDepleted)
                {
                    _game.MarkDeath(DeathCause.FatalFire, preHitLength);
                    return;
                }

                _game.Snake.CommitReduction(lengthChange);
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException("HandleFireContact 執行失敗。", exception);
            }
        }
    }
}

