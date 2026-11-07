using Snake.Application.Repositories;
using Snake.Domain;

namespace Snake.Application._HandleOrdinaryFoodContactUseCase
{
    public class HandleOrdinaryFoodContactUseCase
    {
        private Game _game;

        public HandleOrdinaryFoodContactUseCase(IGameRepository gameRepository)
        {
            ArgumentNullException.ThrowIfNull(gameRepository);
            _game = gameRepository.GetCurrentGame();
        }

        public void HandleOrdinaryFoodContact()
        {
            try
            {
                if (!_game.HasMovedThisTick)
                {
                    return;
                }

                GridPosition head = _game.Snake.BodyCells[0];
                FoodItem? food = _game.Food.ActiveItems.FirstOrDefault(item =>
                    item.Type == FoodType.Ordinary && item.Position.Equals(head));
                if (food is null)
                {
                    return;
                }

                _game.Food.Consume(food.Type);
                _game.Food.MarkPending(FoodType.Ordinary);
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException("HandleOrdinaryFoodContact 執行失敗。", exception);
            }
        }
    }
}

