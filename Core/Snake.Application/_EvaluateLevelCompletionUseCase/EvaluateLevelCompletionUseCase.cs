using Snake.Application.Repositories;
using Snake.Domain;

namespace Snake.Application._EvaluateLevelCompletionUseCase
{
    public class EvaluateLevelCompletionUseCase
    {
        private Game _game;

        public EvaluateLevelCompletionUseCase(IGameRepository gameRepository)
        {
            ArgumentNullException.ThrowIfNull(gameRepository);
            _game = gameRepository.GetCurrentGame();
        }

        public void EvaluateLevelCompletion()
        {
            try
            {
                _game.LevelCompletionPending = _game.Phase == GamePhase.Playing
                    && _game.PendingDeathCause is null
                    && _game.Snake.Length >= _game.Level.TargetLength;
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException("EvaluateLevelCompletion 執行失敗。", exception);
            }
        }
    }
}

