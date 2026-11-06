using Snake.Entities;

namespace Snake.UseCases;

public class SnakeGameSnapshot
{
    internal SnakeGameSnapshot(
        Game game,
        Board board,
        Snake.Entities.Snake snake,
        Level level,
        Food food,
        Fire fire,
        Shield shield,
        Bomb bomb,
        DoorAndMaze doorAndMaze,
        GameResult gameResult)
    {
        Game = game ?? throw new ArgumentNullException(nameof(game));
        Board = board ?? throw new ArgumentNullException(nameof(board));
        Snake = snake ?? throw new ArgumentNullException(nameof(snake));
        Level = level ?? throw new ArgumentNullException(nameof(level));
        Food = food ?? throw new ArgumentNullException(nameof(food));
        Fire = fire ?? throw new ArgumentNullException(nameof(fire));
        Shield = shield ?? throw new ArgumentNullException(nameof(shield));
        Bomb = bomb ?? throw new ArgumentNullException(nameof(bomb));
        DoorAndMaze = doorAndMaze ?? throw new ArgumentNullException(nameof(doorAndMaze));
        GameResult = gameResult ?? throw new ArgumentNullException(nameof(gameResult));
    }

    internal Game Game { get; }
    internal Board Board { get; }
    internal Snake.Entities.Snake Snake { get; }
    internal Level Level { get; }
    internal Food Food { get; }
    internal Fire Fire { get; }
    internal Shield Shield { get; }
    internal Bomb Bomb { get; }
    internal DoorAndMaze DoorAndMaze { get; }
    internal GameResult GameResult { get; }

    internal SnakeGameSnapshot Copy()
    {
        return new SnakeGameSnapshot(
            Game.Copy(),
            Board.Copy(),
            Snake.Copy(),
            Level.Copy(),
            Food.Copy(),
            Fire.Copy(),
            Shield.Copy(),
            Bomb.Copy(),
            DoorAndMaze.Copy(),
            GameResult.Copy());
    }

    internal List<GridPosition> GetObjectOccupiedCells()
    {
        List<GridPosition> occupiedCells = Food.GetOccupiedCells();
        occupiedCells.AddRange(Fire.GetOccupiedCells());
        occupiedCells.AddRange(Shield.GetOccupiedCells());
        occupiedCells.AddRange(Bomb.GetOccupiedCells());
        return occupiedCells;
    }
}
