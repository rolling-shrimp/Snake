using Snake.Entities;

namespace Snake.UseCases;

public class RescueWithShieldUseCase
{
    private SnakeGameSession _session;
    private EndCurrentLevelPlayUseCase _endCurrentLevelPlayUseCase;
    private HashSet<string> _processedDeaths;

    public RescueWithShieldUseCase(
        SnakeGameSession session,
        EndCurrentLevelPlayUseCase endCurrentLevelPlayUseCase)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _endCurrentLevelPlayUseCase = endCurrentLevelPlayUseCase
            ?? throw new ArgumentNullException(nameof(endCurrentLevelPlayUseCase));
        _processedDeaths = new HashSet<string>(StringComparer.Ordinal);
    }

    public RescueWithShieldResult Execute(RescueWithShieldCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        string deathKey = $"{command.DeathCause}:{command.PreHitLength}";
        if (!_session.Game.MovementActive || _processedDeaths.Contains(deathKey))
        {
            return Succeed();
        }

        if (!_session.Shield.CanRescue)
        {
            _endCurrentLevelPlayUseCase.Execute(new EndCurrentLevelPlayCommand(
                LevelOutcome.Failed,
                command.DeathCause));
            _processedDeaths.Add(deathKey);
            return Succeed();
        }

        try
        {
            List<GridPosition> safeCells = _session.Board.GetLegalSpawnCells(
                new List<GridPosition>(),
                _session.GetObjectOccupiedCells());
            if (command.PreHitLength < 1 || safeCells.Count < command.PreHitLength)
            {
                throw new InvalidOperationException("棋盤沒有足夠合法格位安全恢復完整蛇身。");
            }

            if (!_session.Shield.UseCharge())
            {
                throw new InvalidOperationException("護盾次數在保命提交前已失效。");
            }

            _session.Snake.Reset(safeCells.Take(command.PreHitLength).ToList());
            _processedDeaths.Add(deathKey);
            return Succeed();
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException("RescueWithShieldUseCase 執行失敗。", exception);
        }
    }

    private RescueWithShieldResult Succeed()
    {
        return new RescueWithShieldResult(true, null, null);
    }
}
