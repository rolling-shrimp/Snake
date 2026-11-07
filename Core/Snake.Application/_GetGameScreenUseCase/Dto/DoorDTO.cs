using Snake.Application._GetGameScreenUseCase.Enums;

namespace Snake.Application._GetGameScreenUseCase.Dto
{
    public class DoorDTO
    {
        public DoorDTO(GridPositionDTO position, DoorVisualState state)
        {
            Position = position ?? throw new ArgumentNullException(nameof(position));
            State = state;
        }

        public GridPositionDTO Position { get; }
        public DoorVisualState State { get; }
    }
}

