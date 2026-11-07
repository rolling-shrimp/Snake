using Snake.Application.Repositories;
using Snake.Domain;

namespace Snake.Application._HandleSpecialFoodContactUseCase
{
    public class HandleSpecialFoodContactUseCase
    {
        private Game _game;

        public HandleSpecialFoodContactUseCase(IGameRepository gameRepository)
        {
            ArgumentNullException.ThrowIfNull(gameRepository);
            _game = gameRepository.GetCurrentGame();
        }

        public void HandleSpecialFoodContact()
        {
            try
            {
                if (!_game.HasMovedThisTick)
                {
                    return;
                }

                GridPosition head = _game.Snake.BodyCells[0];
                FoodItem? food = _game.Food.ActiveItems.FirstOrDefault(item =>
                    item.Type == FoodType.Special && item.Position.Equals(head));
                if (food is null)
                {
                    return;
                }

                _game.Food.Consume(food.Type);
                _game.Food.MarkPending(FoodType.Special);
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException("HandleSpecialFoodContact 執行失敗。", exception);
            }
        }
    }
}

