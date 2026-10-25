namespace Snake.UseCases;

public interface IAdvancedLevelPlayOutputPort
{
    void HideReadyState(int levelNumber);
    void ShowObject(GameObjectType objectType, GridPositionDTO position);
    void StartMovement(FirstLevelMovementDTO movement);
    void StartDoorCycle(int toggleIntervalMilliseconds, int warningMilliseconds);
}
