using Snake.UseCases;

namespace Snake.Test;

public class StubAdvancedLevelPlayOutputPort : IAdvancedLevelPlayOutputPort
{
    private List<string> _operations = new List<string>();

    public List<string> Operations => new List<string>(_operations);

    public void HideReadyState(int levelNumber)
    {
        _operations.Add($"hide-level-{levelNumber}-ready");
    }

    public void ShowObject(GameObjectType objectType, GridPositionDTO position)
    {
        _operations.Add($"show-{objectType}");
    }

    public void StartMovement(FirstLevelMovementDTO movement)
    {
        _operations.Add("start-movement");
    }

    public void StartDoorCycle(int toggleIntervalMilliseconds, int warningMilliseconds)
    {
        _operations.Add("start-door-cycle");
    }
}
