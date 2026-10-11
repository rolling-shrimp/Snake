namespace Snake.Tests;

public class FixedRandom : Random
{
    private int _nextValue;

    public FixedRandom(int nextValue = 0)
    {
        _nextValue = nextValue;
    }

    public override int Next(int maxValue)
    {
        if (maxValue <= 0)
        {
            return 0;
        }

        return Math.Abs(_nextValue++) % maxValue;
    }
}
