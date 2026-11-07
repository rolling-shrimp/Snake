namespace Snake.Domain
{

public class Level
{
    private LevelNumber _number;

    public Level()
    {
        _number = LevelNumber.First;
    }

    public LevelNumber Number => _number;
    public int TargetLength => _number == LevelNumber.Third ? 50 : 20;
    public BoundaryPolicy BoundaryPolicy => _number == LevelNumber.Third ? BoundaryPolicy.Blocked : BoundaryPolicy.Wrap;

    internal Level Copy()
    {
        Level copy = new Level();
        copy._number = _number;
        return copy;
    }

    public int GetProgressPercent(int snakeLength)
    {
        int progressPercent = snakeLength * 100 / TargetLength;
        return Math.Clamp(progressPercent, 0, 100);
    }

    public bool Advance()
    {
        if (_number == LevelNumber.Third)
        {
            return false;
        }

        _number = (LevelNumber)((int)_number + 1);
        return true;
    }

    public void Reset()
    {
        _number = LevelNumber.First;
    }
}
}
