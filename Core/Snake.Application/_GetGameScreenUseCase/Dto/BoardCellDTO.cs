using Snake.Application._GetGameScreenUseCase.Enums;

namespace Snake.Application._GetGameScreenUseCase.Dto
{
    public class BoardCellDTO
    {
        public BoardCellDTO(GridPositionDTO position, BoardCellKind kind)
        {
            Position = position ?? throw new ArgumentNullException(nameof(position));
            Kind = kind;
        }

        public GridPositionDTO Position { get; }
        public BoardCellKind Kind { get; }
    }
}

