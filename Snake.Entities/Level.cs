namespace Snake.Entities;

public class Level
{
    private LevelId _levelId;
    private LevelNumber _number;

    public Level(LevelId levelId)
    {
        _levelId = levelId ?? throw new ArgumentNullException(nameof(levelId));
        _number = LevelNumber.First;
    }

    public LevelId LevelId => _levelId;
    public LevelNumber Number => _number;
    public int TargetLength => _number == LevelNumber.Third ? 50 : 20;
    public BoundaryPolicy BoundaryPolicy => _number == LevelNumber.Third ? BoundaryPolicy.Blocked : BoundaryPolicy.Wrap;

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
