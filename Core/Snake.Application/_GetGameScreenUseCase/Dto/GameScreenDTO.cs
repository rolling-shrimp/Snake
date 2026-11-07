using Snake.Application._GetGameScreenUseCase.Enums;

namespace Snake.Application._GetGameScreenUseCase.Dto
{
    public class GameScreenDTO
    {
        public GameScreenDTO(
            GameScreenKind screenKind,
            List<BoardCellDTO> boardCells,
            List<GridPositionDTO> snakeCells,
            List<BoardObjectDTO> objects,
            List<GridPositionDTO> wallCells,
            List<DoorDTO> doors,
            LevelStatusDTO levelStatus,
            List<string> ruleHints,
            ResultCardDTO? result,
            List<GameAction> availableActions)
        {
            ScreenKind = screenKind;
            BoardCells = boardCells ?? throw new ArgumentNullException(nameof(boardCells));
            SnakeCells = snakeCells ?? throw new ArgumentNullException(nameof(snakeCells));
            Objects = objects ?? throw new ArgumentNullException(nameof(objects));
            WallCells = wallCells ?? throw new ArgumentNullException(nameof(wallCells));
            Doors = doors ?? throw new ArgumentNullException(nameof(doors));
            LevelStatus = levelStatus ?? throw new ArgumentNullException(nameof(levelStatus));
            RuleHints = ruleHints ?? throw new ArgumentNullException(nameof(ruleHints));
            Result = result;
            AvailableActions = availableActions ?? throw new ArgumentNullException(nameof(availableActions));
        }

        public GameScreenKind ScreenKind { get; }
        public List<BoardCellDTO> BoardCells { get; }
        public List<GridPositionDTO> SnakeCells { get; }
        public List<BoardObjectDTO> Objects { get; }
        public List<GridPositionDTO> WallCells { get; }
        public List<DoorDTO> Doors { get; }
        public LevelStatusDTO LevelStatus { get; }
        public List<string> RuleHints { get; }
        public ResultCardDTO? Result { get; }
        public List<GameAction> AvailableActions { get; }
    }
}

