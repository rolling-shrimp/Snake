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

    internal DoorAndMaze Copy()
    {
        DoorAndMaze copy = new DoorAndMaze(_doorAndMazeId);
        copy._wallCells = new List<GridPosition>(_wallCells);
        copy._doors = _doors.Select(door => door.Copy()).ToList();
        return copy;
    }

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

    public void ApplyClosingWarning()
    {
        foreach (Door door in _doors)
        {
            if (door.Phase == DoorPhase.Open)
            {
                door.ChangePhase(DoorPhase.ClosingWarning);
            }
        }
    }

    public bool ApplyToggle(List<GridPosition> snakeBodyCells)
    {
        ArgumentNullException.ThrowIfNull(snakeBodyCells);
        bool wasDeferred = false;
        foreach (Door door in _doors)
        {
            if (door.Phase == DoorPhase.Closed)
            {
                door.ChangePhase(DoorPhase.Open);
                continue;
            }

            if (snakeBodyCells.Contains(door.Position))
            {
                door.ChangePhase(DoorPhase.CloseDeferred);
                wasDeferred = true;
            }
            else
            {
                door.ChangePhase(DoorPhase.Closed);
            }
        }

        return wasDeferred;
    }

    public bool CloseDeferredDoors(List<GridPosition> snakeBodyCells)
    {
        ArgumentNullException.ThrowIfNull(snakeBodyCells);
        bool closedAnyDoor = false;
        foreach (Door door in _doors)
        {
            if (door.Phase == DoorPhase.CloseDeferred
                && !snakeBodyCells.Contains(door.Position))
            {
                door.ChangePhase(DoorPhase.Closed);
                closedAnyDoor = true;
            }
        }

        return closedAnyDoor;
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
