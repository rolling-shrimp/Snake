using Snake.Application.Repositories;
using Snake.Application._SetSnakeDirectionUseCase.Dto;
using Snake.Domain;

namespace Snake.Application._SetSnakeDirectionUseCase
{
    public class SetSnakeDirectionUseCase
    {
        private Game _game;

        public SetSnakeDirectionUseCase(IGameRepository gameRepository)
        {
            ArgumentNullException.ThrowIfNull(gameRepository);
            _game = gameRepository.GetCurrentGame();
        }

        public void SetSnakeDirection(SnakeDirectionDTO snakeDirection)
        {
            ArgumentNullException.ThrowIfNull(snakeDirection);
            try
            {
                if (_game.Phase != GamePhase.Playing)
                {
                    return;
                }

                Direction direction = MapDirection(snakeDirection);
                _game.Snake.QueueDirection(direction);
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException("SetSnakeDirection 執行失敗。", exception);
            }
        }

        private Direction MapDirection(SnakeDirectionDTO snakeDirection)
        {
            return (snakeDirection.X, snakeDirection.Y) switch
            {
                (0, -1) => Direction.Up,
                (0, 1) => Direction.Down,
                (-1, 0) => Direction.Left,
                (1, 0) => Direction.Right,
                _ => throw new ArgumentOutOfRangeException(nameof(snakeDirection), "方向必須是單一水平或垂直單位向量。")
            };
        }
    }
}

