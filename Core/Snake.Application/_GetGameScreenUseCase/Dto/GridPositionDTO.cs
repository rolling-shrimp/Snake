namespace Snake.Application._GetGameScreenUseCase.Dto
{
    public class GridPositionDTO
    {
        public GridPositionDTO(int x, int y)
        {
            X = x;
            Y = y;
        }

        public int X { get; }
        public int Y { get; }
    }
}
