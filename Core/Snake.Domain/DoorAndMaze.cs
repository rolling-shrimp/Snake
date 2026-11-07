namespace Snake.Domain
{

public class DoorAndMaze
{
    private List<GridPosition> _wallCells;
    private List<Door> _doors;

    public DoorAndMaze()
    {
        _wallCells = new List<GridPosition>();
        _doors = new List<Door>();
    }

    public List<GridPosition> WallCells => new List<GridPosition>(_wallCells);
    public List<Door> Doors => new List<Door>(_doors);

    internal DoorAndMaze Copy()
    {
        DoorAndMaze copy = new DoorAndMaze();
        copy._wallCells = new List<GridPosition>(_wallCells);
        copy._doors = _doors.Select(door => door.Copy()).ToList();
        return copy;
    }

    public void AddDoor(GridPosition position)
    {
        ArgumentNullException.ThrowIfNull(position);
        if (_doors.Any(door => door.Position.Equals(position)))
        {
            return;
        }

        _doors.Add(new Door(position));
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

    public void PrepareThirdLevel()
    {
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
        AddDoor(new GridPosition(5, 5));
    }
}
}
