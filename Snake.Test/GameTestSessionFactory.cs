using Snake.Entities;
using Snake.UseCases;

namespace Snake.Test;

public class GameTestSessionFactory
{
    public SnakeGameSession CreatePlaying(LevelNumber levelNumber, int snakeLength = 3)
    {
        SnakeGameSession session = CreateReady(levelNumber, snakeLength);
        session.Game.ActivateFirstLevel();
        return session;
    }

    public SnakeGameSession CreateReady(LevelNumber levelNumber, int snakeLength = 3)
    {
        Board board = new Board(new BoardId(Guid.NewGuid()));
        Snake.Entities.Snake snake = new Snake.Entities.Snake(
            new SnakeId(Guid.NewGuid()),
            board.GetFirstLevelSafeStart());
        if (snakeLength != 3)
        {
            snake.Reset(CreateBody(snakeLength));
        }

        Level level = new Level(new LevelId(Guid.NewGuid()));
        while (level.Number != levelNumber)
        {
            level.Advance();
        }

        return new SnakeGameSession(
            new Game(new GameId(Guid.NewGuid())),
            board,
            snake,
            level,
            new Food(new FoodId(Guid.NewGuid())));
    }

    public List<GridPosition> CreateBody(int length)
    {
        if (length < 0 || length > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(length));
        }

        List<GridPosition> body = new List<GridPosition>();
        for (int index = 0; index < length; index++)
        {
            int y = index / 10;
            int column = index % 10;
            int x = y % 2 == 0 ? column : 9 - column;
            body.Add(new GridPosition(x, y));
        }

        return body;
    }
}
