namespace Snake.Application._SetSnakeDirectionUseCase.Dto
{
    public class SnakeDirectionDTO
    {
        public SnakeDirectionDTO(int x, int y)
        {
            X = x;
            Y = y;
        }

        public int X { get; }
        public int Y { get; }
    }
}
