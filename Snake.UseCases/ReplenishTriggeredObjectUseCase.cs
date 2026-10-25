using Snake.Entities;

namespace Snake.UseCases;

public class ReplenishTriggeredObjectUseCase
{
    private SnakeGameSession _session;
    private HashSet<string> _processedItems;

    public ReplenishTriggeredObjectUseCase(SnakeGameSession session)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _processedItems = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    }

    public ReplenishTriggeredObjectResult Execute(ReplenishTriggeredObjectCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (!_session.Game.MovementActive)
        {
            ClearPending();
            return Succeed();
        }

        string processKey = $"{command.ObjectType}:{command.PreviousItemId}";
        if (_processedItems.Contains(processKey) || !IsAllowed(command.ObjectType))
        {
            return Succeed();
        }

        List<GridPosition> legalCells = _session.Board.GetLegalSpawnCells(
            _session.Snake.BodyCells,
            _session.GetObjectOccupiedCells());
        if (legalCells.Count == 0)
        {
            MarkPending(command.ObjectType);
            _processedItems.Add(processKey);
            return Succeed();
        }

        Spawn(command.ObjectType, legalCells[0]);
        _processedItems.Add(processKey);
        return Succeed();
    }

    private bool IsAllowed(GameObjectType objectType)
    {
        return objectType switch
        {
            GameObjectType.Strawberry => true,
            GameObjectType.SpecialFood => _session.Level.Number != LevelNumber.First,
            GameObjectType.Shield => _session.Level.Number != LevelNumber.First,
            GameObjectType.Fire => _session.Level.Number != LevelNumber.First,
            GameObjectType.Bomb => _session.Level.Number == LevelNumber.Third,
            _ => false
        };
    }

    private void Spawn(GameObjectType objectType, GridPosition position)
    {
        switch (objectType)
        {
            case GameObjectType.Strawberry:
                _session.Food.SpawnOrdinary(new FoodItemId(Guid.NewGuid()), position);
                break;
            case GameObjectType.SpecialFood:
                _session.Food.SpawnSpecial(new FoodItemId(Guid.NewGuid()), position);
                break;
            case GameObjectType.Shield:
                _session.Shield.Spawn(new ShieldItemId(Guid.NewGuid()), position);
                break;
            case GameObjectType.Fire:
                _session.Fire.Spawn(new FireItemId(Guid.NewGuid()), position);
                break;
            case GameObjectType.Bomb:
                _session.Bomb.Spawn(new BombItemId(Guid.NewGuid()), position);
                break;
        }
    }

    private void MarkPending(GameObjectType objectType)
    {
        switch (objectType)
        {
            case GameObjectType.Strawberry:
                _session.Food.MarkPending(FoodType.Ordinary);
                break;
            case GameObjectType.SpecialFood:
                _session.Food.MarkPending(FoodType.Special);
                break;
            case GameObjectType.Shield:
                _session.Shield.MarkPending();
                break;
            case GameObjectType.Fire:
                _session.Fire.MarkPending();
                break;
            case GameObjectType.Bomb:
                _session.Bomb.MarkPending();
                break;
        }
    }

    private void ClearPending()
    {
        _session.Food.ClearPending();
        _session.Shield.ClearPending();
        _session.Fire.ClearPending();
        _session.Bomb.ClearPending();
    }

    private ReplenishTriggeredObjectResult Succeed()
    {
        return new ReplenishTriggeredObjectResult(true, null, null);
    }
}
