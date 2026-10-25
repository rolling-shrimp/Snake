using Snake.Entities;

namespace Snake.UseCases;

public class ActivateFirstLevelPlayUseCase
{
    private SnakeGameSession _session;
    private IFirstLevelPlayOutputPort _outputPort;

    public ActivateFirstLevelPlayUseCase(SnakeGameSession session, IFirstLevelPlayOutputPort outputPort)
    {
        _session = session;
        _outputPort = outputPort;
    }

    public ActivateFirstLevelPlayResult Execute(ActivateFirstLevelPlayCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (_session.Game.Phase != GamePhase.Ready || _session.Level.Number != LevelNumber.First || _session.Food.ActiveItems.Count != 0)
        {
            return new ActivateFirstLevelPlayResult(true, null, null);
        }

        try
        {
            List<GridPosition> legalCells = _session.Board.GetLegalSpawnCells(
                _session.Snake.BodyCells,
                _session.Food.GetOccupiedCells());
            if (legalCells.Count == 0)
            {
                throw new InvalidOperationException("第一關沒有可產生普通食物的合法格位。");
            }

            GridPosition foodPosition = legalCells[0];
            _session.Game.ActivateFirstLevel();
            _session.Food.SpawnOrdinary(new FoodItemId(Guid.NewGuid()), foodPosition);

            _outputPort.HideStartButton();
            _outputPort.ShowOrdinaryFood(new FirstLevelFoodDTO(
                GameObjectType.Strawberry,
                new GridPositionDTO(foodPosition.X, foodPosition.Y)));
            _outputPort.StartMovement(new FirstLevelMovementDTO(
                _session.Game.MovementActive,
                _session.Snake.MovementIntervalMilliseconds,
                _session.Snake.LevelTimeLimitMilliseconds));
            return new ActivateFirstLevelPlayResult(true, null, null);
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException("ActivateFirstLevelPlayUseCase 執行失敗。", exception);
        }
    }
}
