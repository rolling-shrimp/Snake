using Snake.Application._GetGameScreenUseCase.Enums;

namespace Snake.Application._GetGameScreenUseCase.Dto
{
    public class BoardObjectDTO
    {
        public BoardObjectDTO(BoardObjectKind kind, GridPositionDTO position)
        {
            Kind = kind;
            Position = position ?? throw new ArgumentNullException(nameof(position));
        }

        public BoardObjectKind Kind { get; }
        public GridPositionDTO Position { get; }
    }
}

