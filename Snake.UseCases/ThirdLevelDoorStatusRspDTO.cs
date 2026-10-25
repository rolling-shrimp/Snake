namespace Snake.UseCases;

public class ThirdLevelDoorStatusRspDTO
{
    private List<DoorStatusDTO> _doors;

    public ThirdLevelDoorStatusRspDTO(List<DoorStatusDTO> doors, bool isCycleActive)
    {
        _doors = new List<DoorStatusDTO>(doors);
        IsCycleActive = isCycleActive;
    }

    public List<DoorStatusDTO> Doors => new List<DoorStatusDTO>(_doors);
    public bool IsCycleActive { get; }
}
