using Snake.Entities;

namespace Snake.UseCases;

public class SnakeGameSession : IGameSnapshotStore
{
    private Game _game;
    private Board _board;
    private Snake.Entities.Snake _snake;
    private Level _level;
    private Food _food;
    private Fire _fire;
    private Shield _shield;
    private Bomb _bomb;
    private DoorAndMaze _doorAndMaze;
    private GameResult _gameResult;
    private long _version;
    private object _syncRoot;

    public SnakeGameSession()
    {
        _board = new Board(new BoardId(Guid.NewGuid()));
        _snake = new Snake.Entities.Snake(
            new SnakeId(Guid.NewGuid()),
            _board.GetFirstLevelSafeStart());
        _level = new Level(new LevelId(Guid.NewGuid()));
        _food = new Food(new FoodId(Guid.NewGuid()));
        _fire = new Fire(new FireId(Guid.NewGuid()));
        _shield = new Shield(new ShieldId(Guid.NewGuid()));
        _bomb = new Bomb(new BombId(Guid.NewGuid()));
        _doorAndMaze = new DoorAndMaze(new DoorAndMazeId(Guid.NewGuid()));
        _gameResult = new GameResult(new GameResultId(Guid.NewGuid()));
        _game = new Game(new GameId(Guid.NewGuid()));
        _version = 0;
        _syncRoot = new object();
    }

    public VersionedSnakeGameSnapshot Load()
    {
        lock (_syncRoot)
        {
            return new VersionedSnakeGameSnapshot(_version, CreateSnapshot());
        }
    }

    public bool TryCommit(long expectedVersion, SnakeGameSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        lock (_syncRoot)
        {
            if (_version != expectedVersion)
            {
                return false;
            }

            SnakeGameSnapshot committedSnapshot = snapshot.Copy();
            _game = committedSnapshot.Game;
            _board = committedSnapshot.Board;
            _snake = committedSnapshot.Snake;
            _level = committedSnapshot.Level;
            _food = committedSnapshot.Food;
            _fire = committedSnapshot.Fire;
            _shield = committedSnapshot.Shield;
            _bomb = committedSnapshot.Bomb;
            _doorAndMaze = committedSnapshot.DoorAndMaze;
            _gameResult = committedSnapshot.GameResult;
            _version++;
            return true;
        }
    }

    private SnakeGameSnapshot CreateSnapshot()
    {
        return new SnakeGameSnapshot(
            _game.Copy(),
            _board.Copy(),
            _snake.Copy(),
            _level.Copy(),
            _food.Copy(),
            _fire.Copy(),
            _shield.Copy(),
            _bomb.Copy(),
            _doorAndMaze.Copy(),
            _gameResult.Copy());
    }
}
