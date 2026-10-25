using Snake.UseCases;

namespace Snake.Test;

public class RecordingFirstLevelPlayOutputPort : IFirstLevelPlayOutputPort
{
    private List<string> _operations = new List<string>();
    private FirstLevelFoodDTO? _food;
    private FirstLevelMovementDTO? _movement;

    public List<string> Operations => new List<string>(_operations);
    public FirstLevelFoodDTO? Food => _food;
    public FirstLevelMovementDTO? Movement => _movement;
    public int ActivationCount => _operations.Count(operation => operation == "start_movement");

    public void HideStartButton()
    {
        _operations.Add("hide_start_button");
    }

    public void ShowOrdinaryFood(FirstLevelFoodDTO food)
    {
        _food = food;
        _operations.Add("spawn_ordinary_food");
    }

    public void StartMovement(FirstLevelMovementDTO movement)
    {
        _movement = movement;
        _operations.Add("start_movement");
    }
}
