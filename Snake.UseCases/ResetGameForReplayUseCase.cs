using Snake.Entities;

namespace Snake.UseCases;

public class ResetGameForReplayUseCase
{
    private SnakeGameSession _session;
    private IAdvancedLevelPlayOutputPort _outputPort;

    public ResetGameForReplayUseCase(
        SnakeGameSession session,
        IAdvancedLevelPlayOutputPort outputPort)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _outputPort = outputPort ?? throw new ArgumentNullException(nameof(outputPort));
    }

    public ResetGameForReplayResult Execute(ResetGameForReplayCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (_session.Game.Phase != GamePhase.GameOver
            && _session.Game.Phase != GamePhase.Completed)
        {
            return Succeed();
        }

        try
        {
            _session.Level.Reset();
            _session.Snake.Reset(_session.Board.GetFirstLevelSafeStart());
            _session.Food.Reset();
            _session.Shield.Reset();
            _session.Fire.Reset();
            _session.Bomb.Reset();
            _session.DoorAndMaze.Reset();
            _session.GameResult.Reset();

            GridPosition foodPosition = _session.Board.GetLegalSpawnCells(
                _session.Snake.BodyCells,
                _session.GetObjectOccupiedCells())[0];
            _session.Food.SpawnOrdinary(new FoodItemId(Guid.NewGuid()), foodPosition);
            _session.Game.RestartPlaying();

            _outputPort.HideReadyState(1);
            _outputPort.ShowObject(
                GameObjectType.Strawberry,
                new GridPositionDTO(foodPosition.X, foodPosition.Y));
            _outputPort.StartMovement(new FirstLevelMovementDTO(
                _session.Game.MovementActive,
                _session.Snake.MovementIntervalMilliseconds,
                _session.Snake.LevelTimeLimitMilliseconds));
            return Succeed();
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException("ResetGameForReplayUseCase 執行失敗。", exception);
        }
    }

    private ResetGameForReplayResult Succeed()
    {
        return new ResetGameForReplayResult(true, null, null);
    }
}
