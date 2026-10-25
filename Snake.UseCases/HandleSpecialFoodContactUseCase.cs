using Snake.Entities;

namespace Snake.UseCases;

public class HandleSpecialFoodContactUseCase
{
    private SnakeGameSession _session;

    public HandleSpecialFoodContactUseCase(SnakeGameSession session)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
    }

    public HandleSpecialFoodContactResult Execute(HandleSpecialFoodContactCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (!_session.Game.MovementActive
            || _session.Level.Number == LevelNumber.First
            || !Guid.TryParse(command.FoodItemId, out Guid itemIdValue))
        {
            return Succeed();
        }

        FoodItemId foodItemId = new FoodItemId(itemIdValue);
        FoodItem? foodItem = _session.Food.ActiveItems.FirstOrDefault(item =>
            item.FoodItemId.Equals(foodItemId) && item.Type == FoodType.Special);
        if (foodItem is null || !foodItem.Position.Equals(_session.Snake.BodyCells[0]))
        {
            return Succeed();
        }

        try
        {
            _session.Food.Consume(foodItemId, FoodType.Special);
            int previousLength = _session.Snake.Length;
            List<GridPosition> legalCells = _session.Board.GetLegalSpawnCells(
                _session.Snake.BodyCells,
                _session.GetObjectOccupiedCells());
            if (legalCells.Count < _session.Food.SpecialLengthDelta)
            {
                throw new InvalidOperationException("沒有足夠合法格位套用特殊食物成長效果。");
            }

            _session.Snake.Grow(legalCells.Take(_session.Food.SpecialLengthDelta).ToList());
            _session.PublishFact(new FoodConsumedDomainEvent(foodItemId, FoodType.Special));
            _session.PublishFact(new SnakeLengthChangedDomainEvent(previousLength, _session.Snake.Length));
            return Succeed();
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException("HandleSpecialFoodContactUseCase 執行失敗。", exception);
        }
    }

    private HandleSpecialFoodContactResult Succeed()
    {
        return new HandleSpecialFoodContactResult(true, null, null);
    }
}
