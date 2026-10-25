namespace Snake.Entities;

public class DoorAndMaze
{
    private const int TOGGLE_INTERVAL_MILLISECONDS = 6000;
    private const int CLOSING_WARNING_MILLISECONDS = 2000;

    private DoorAndMazeId _doorAndMazeId;
    private List<GridPosition> _wallCells;
    private List<Door> _doors;

    public DoorAndMaze(DoorAndMazeId doorAndMazeId)
    {
        _doorAndMazeId = doorAndMazeId ?? throw new ArgumentNullException(nameof(doorAndMazeId));
        _wallCells = new List<GridPosition>();
        _doors = new List<Door>();
    }

    public DoorAndMazeId DoorAndMazeId => _doorAndMazeId;
    public List<GridPosition> WallCells => new List<GridPosition>(_wallCells);
    public List<Door> Doors => new List<Door>(_doors);
    public int ToggleIntervalMilliseconds => TOGGLE_INTERVAL_MILLISECONDS;
    public int ClosingWarningMilliseconds => CLOSING_WARNING_MILLISECONDS;

    public void AddDoor(DoorId doorId, GridPosition position)
    {
        ArgumentNullException.ThrowIfNull(doorId);
        ArgumentNullException.ThrowIfNull(position);
        if (_doors.Any(door => door.DoorId.Equals(doorId) || door.Position.Equals(position)))
        {
            return;
        }

        _doors.Add(new Door(doorId, position));
    }

    public Door? FindDoor(DoorId doorId)
    {
        ArgumentNullException.ThrowIfNull(doorId);
        return _doors.FirstOrDefault(door => door.DoorId.Equals(doorId));
    }

    public void Update(int elapsedMilliseconds, List<GridPosition> snakeBodyCells)
    {
        ArgumentNullException.ThrowIfNull(snakeBodyCells);

        foreach (Door door in _doors)
        {
            bool isOccupied = snakeBodyCells.Contains(door.Position);
            if (door.Phase == DoorPhase.CloseDeferred)
            {
                if (!isOccupied)
                {
                    door.ChangePhase(DoorPhase.Closed);
                }

                continue;
            }

            if (elapsedMilliseconds >= TOGGLE_INTERVAL_MILLISECONDS)
            {
                if (door.Phase == DoorPhase.Closed)
                {
                    door.ChangePhase(DoorPhase.Open);
                }
                else if (isOccupied)
                {
                    door.ChangePhase(DoorPhase.CloseDeferred);
                }
                else
                {
                    door.ChangePhase(DoorPhase.Closed);
                }

                continue;
            }

            int warningStart = TOGGLE_INTERVAL_MILLISECONDS - CLOSING_WARNING_MILLISECONDS;
            if (elapsedMilliseconds >= warningStart && door.Phase == DoorPhase.Open)
            {
                door.ChangePhase(DoorPhase.ClosingWarning);
            }
        }
    }

    public List<GridPosition> GetUnavailableCells()
    {
        List<GridPosition> cells = new List<GridPosition>(_wallCells);
        cells.AddRange(_doors.Select(door => door.Position));
        return cells;
    }

    public void Reset()
    {
        _wallCells.Clear();
        _doors.Clear();
    }

    public void PrepareThirdLevel(DoorId doorId)
    {
        ArgumentNullException.ThrowIfNull(doorId);
        Reset();
        for (int coordinate = 0; coordinate < 10; coordinate++)
        {
            _wallCells.Add(new GridPosition(coordinate, 0));
            _wallCells.Add(new GridPosition(coordinate, 9));
            if (coordinate > 0 && coordinate < 9)
            {
                _wallCells.Add(new GridPosition(0, coordinate));
                _wallCells.Add(new GridPosition(9, coordinate));
            }
        }

        _wallCells.Add(new GridPosition(5, 2));
        _wallCells.Add(new GridPosition(5, 3));
        _wallCells.Add(new GridPosition(5, 4));
        _wallCells.Add(new GridPosition(5, 6));
        _wallCells.Add(new GridPosition(5, 7));
        AddDoor(doorId, new GridPosition(5, 5));
    }
}
