namespace Snake.UseCases;

public class FirstLevelFoodDTO
{
    public FirstLevelFoodDTO(GameObjectType objectType, GridPositionDTO position)
    {
        ObjectType = objectType;
        Position = position;
    }

    public GameObjectType ObjectType { get; }
    public GridPositionDTO Position { get; }
}
