using Snake.Entities;

namespace Snake.UseCases;

public class ActivateSecondLevelPlayUseCase
{
    private SnakeGameSession _session;
    private IAdvancedLevelPlayOutputPort _outputPort;

    public ActivateSecondLevelPlayUseCase(
        SnakeGameSession session,
        IAdvancedLevelPlayOutputPort outputPort)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _outputPort = outputPort ?? throw new ArgumentNullException(nameof(outputPort));
    }

    public ActivateSecondLevelPlayResult Execute(ActivateSecondLevelPlayCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (_session.Game.Phase != GamePhase.Ready
            || _session.Level.Number != LevelNumber.Second
            || _session.GetObjectOccupiedCells().Count != 0)
        {
            return Succeed();
        }

        try
        {
            _outputPort.HideReadyState(2);
            GridPosition ordinaryPosition = GetNextLegalCell();
            _session.Food.SpawnOrdinary(new FoodItemId(Guid.NewGuid()), ordinaryPosition);
            ShowObject(GameObjectType.Strawberry, ordinaryPosition);

            GridPosition specialPosition = GetNextLegalCell();
            _session.Food.SpawnSpecial(new FoodItemId(Guid.NewGuid()), specialPosition);
            ShowObject(GameObjectType.SpecialFood, specialPosition);

            GridPosition shieldPosition = GetNextLegalCell();
            _session.Shield.Spawn(new ShieldItemId(Guid.NewGuid()), shieldPosition);
            ShowObject(GameObjectType.Shield, shieldPosition);

            GridPosition firePosition = GetNextLegalCell();
            _session.Fire.Spawn(new FireItemId(Guid.NewGuid()), firePosition);
            ShowObject(GameObjectType.Fire, firePosition);

            _session.Game.ActivateLevel();
            _outputPort.StartMovement(new FirstLevelMovementDTO(
                _session.Game.MovementActive,
                _session.Snake.MovementIntervalMilliseconds,
                _session.Snake.LevelTimeLimitMilliseconds));
            return Succeed();
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException("ActivateSecondLevelPlayUseCase 執行失敗。", exception);
        }
    }

    private GridPosition GetNextLegalCell()
    {
        List<GridPosition> legalCells = _session.Board.GetLegalSpawnCells(
            _session.Snake.BodyCells,
            _session.GetObjectOccupiedCells());
        if (legalCells.Count == 0)
        {
            throw new InvalidOperationException("第二關沒有足夠合法格位產生遊玩物件。");
        }

        return legalCells[0];
    }

    private void ShowObject(GameObjectType objectType, GridPosition position)
    {
        _outputPort.ShowObject(objectType, new GridPositionDTO(position.X, position.Y));
    }

    private ActivateSecondLevelPlayResult Succeed()
    {
        return new ActivateSecondLevelPlayResult(true, null, null);
    }
}
