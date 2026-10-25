using Snake.Entities;

namespace Snake.UseCases;

public class EndCurrentLevelPlayUseCase
{
    private SnakeGameSession _session;
    private PrepareNextLevelUseCase _prepareNextLevelUseCase;
    private HashSet<LevelNumber> _processedLevels;

    public EndCurrentLevelPlayUseCase(
        SnakeGameSession session,
        PrepareNextLevelUseCase prepareNextLevelUseCase)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _prepareNextLevelUseCase = prepareNextLevelUseCase
            ?? throw new ArgumentNullException(nameof(prepareNextLevelUseCase));
        _processedLevels = new HashSet<LevelNumber>();
    }

    public EndCurrentLevelPlayResult Execute(EndCurrentLevelPlayCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        LevelNumber completedLevel = _session.Level.Number;
        if (!_session.Game.MovementActive || _processedLevels.Contains(completedLevel))
        {
            return Succeed();
        }

        if (command.Outcome == LevelOutcome.Passed)
        {
            _session.GameResult.Record(
                new LevelResultId(Guid.NewGuid()),
                completedLevel,
                Snake.Entities.LevelOutcome.Passed,
                _session.Snake.Length,
                null);
            _processedLevels.Add(completedLevel);
            if (completedLevel == LevelNumber.Third)
            {
                _session.Game.Complete();
                return Succeed();
            }

            _prepareNextLevelUseCase.Execute(new PrepareNextLevelCommand((int)completedLevel));
            return Succeed();
        }

        if (command.DeathCause is null)
        {
            return Succeed();
        }

        Snake.Entities.DeathCause deathCause = MapDeathCause(command.DeathCause.Value);
        int finalLength = _session.Snake.Length;
        if (deathCause == Snake.Entities.DeathCause.FatalFire)
        {
            _session.Snake.Deplete();
            finalLength = 0;
        }

        _session.GameResult.Record(
            new LevelResultId(Guid.NewGuid()),
            completedLevel,
            Snake.Entities.LevelOutcome.Failed,
            finalLength,
            deathCause);
        _session.Game.EndGame();
        _processedLevels.Add(completedLevel);
        return Succeed();
    }

    private Snake.Entities.DeathCause MapDeathCause(DeathCause deathCause)
    {
        return deathCause switch
        {
            DeathCause.SelfCollision => Snake.Entities.DeathCause.SelfCollision,
            DeathCause.WallCollision => Snake.Entities.DeathCause.WallCollision,
            DeathCause.ClosedDoorCollision => Snake.Entities.DeathCause.ClosedDoorCollision,
            DeathCause.Bomb => Snake.Entities.DeathCause.Bomb,
            DeathCause.FatalFire => Snake.Entities.DeathCause.FatalFire,
            _ => throw new ArgumentOutOfRangeException(nameof(deathCause), deathCause, "不支援的死亡原因。")
        };
    }

    private EndCurrentLevelPlayResult Succeed()
    {
        return new EndCurrentLevelPlayResult(true, null, null);
    }
}
