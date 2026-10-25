using Snake.Entities;

namespace Snake.UseCases;

public class HandleOrdinaryFoodContactUseCase
{
    private SnakeGameSession _session;

    public HandleOrdinaryFoodContactUseCase(SnakeGameSession session)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
    }

    public HandleOrdinaryFoodContactResult Execute(HandleOrdinaryFoodContactCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (!_session.Game.MovementActive || !Guid.TryParse(command.FoodItemId, out Guid itemIdValue))
        {
            return Succeed();
        }

        FoodItemId foodItemId = new FoodItemId(itemIdValue);
        FoodItem? foodItem = _session.Food.ActiveItems.FirstOrDefault(item =>
            item.FoodItemId.Equals(foodItemId) && item.Type == FoodType.Ordinary);
        if (foodItem is null || !foodItem.Position.Equals(_session.Snake.BodyCells[0]))
        {
            return Succeed();
        }

        try
        {
            _session.Food.Consume(foodItemId, FoodType.Ordinary);
            ApplyGrowth(foodItemId, FoodType.Ordinary, _session.Food.OrdinaryLengthDelta);
            return Succeed();
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException("HandleOrdinaryFoodContactUseCase 執行失敗。", exception);
        }
    }

    private void ApplyGrowth(FoodItemId foodItemId, FoodType foodType, int lengthDelta)
    {
        int previousLength = _session.Snake.Length;
        List<GridPosition> legalCells = _session.Board.GetLegalSpawnCells(
            _session.Snake.BodyCells,
            _session.GetObjectOccupiedCells());
        if (legalCells.Count < lengthDelta)
        {
            throw new InvalidOperationException("沒有足夠合法格位套用食物成長效果。");
        }

        _session.Snake.Grow(legalCells.Take(lengthDelta).ToList());
        _session.PublishFact(new FoodConsumedDomainEvent(foodItemId, foodType));
        _session.PublishFact(new SnakeLengthChangedDomainEvent(previousLength, _session.Snake.Length));
    }

    private HandleOrdinaryFoodContactResult Succeed()
    {
        return new HandleOrdinaryFoodContactResult(true, null, null);
    }
}
