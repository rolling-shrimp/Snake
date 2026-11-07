using Snake.Application.Repositories;
using Snake.Domain;

namespace Snake.Application._EndCurrentLevelPlayUseCase
{
    public class EndCurrentLevelPlayUseCase
    {
        private Game _game;

        public EndCurrentLevelPlayUseCase(IGameRepository gameRepository)
        {
            ArgumentNullException.ThrowIfNull(gameRepository);
            _game = gameRepository.GetCurrentGame();
        }

        public void EndCurrentLevelPlay()
        {
            try
            {
                if (_game.Phase != GamePhase.Playing)
                {
                    return;
                }

                if (_game.PendingDeathCause is not null)
                {
                    EndWithFailure(_game.PendingDeathCause.Value);
                    return;
                }

                if (!_game.LevelCompletionPending)
                {
                    return;
                }

                RecordResult(LevelOutcome.Passed, _game.Snake.Length, null);
                if (_game.Level.Number == LevelNumber.Third)
                {
                    _game.Complete();
                    return;
                }

                _game.PrepareNextLevel();
                _game.NextLevelPreparationPending = true;
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException("EndCurrentLevelPlay 執行失敗。", exception);
            }
        }

        private void EndWithFailure(DeathCause deathCause)
        {
            if (deathCause == DeathCause.FatalFire)
            {
                _game.Snake.Deplete();
            }

            RecordResult(LevelOutcome.Failed, Math.Max(0, _game.Snake.Length), deathCause);
            _game.EndGame();
        }

        private void RecordResult(LevelOutcome outcome, int finalLength, DeathCause? deathCause)
        {
            bool recorded = _game.GameResult.Record(
                _game.Level.Number,
                outcome,
                finalLength,
                deathCause);
            if (!recorded)
            {
                throw new InvalidOperationException("目前關卡已經存在結果，不能重複結束。");
            }
        }
    }
}

