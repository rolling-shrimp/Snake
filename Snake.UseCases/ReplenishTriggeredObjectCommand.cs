namespace Snake.UseCases;

public class ReplenishTriggeredObjectCommand
{
    public ReplenishTriggeredObjectCommand(GameObjectType objectType, string previousItemId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(previousItemId);
        ObjectType = objectType;
        PreviousItemId = previousItemId;
    }

    public GameObjectType ObjectType { get; }
    public string PreviousItemId { get; }
}
