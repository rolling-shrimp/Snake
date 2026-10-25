namespace Snake.UseCases;

public interface IFirstLevelPlayOutputPort
{
    void HideStartButton();
    void ShowOrdinaryFood(FirstLevelFoodDTO food);
    void StartMovement(FirstLevelMovementDTO movement);
}
