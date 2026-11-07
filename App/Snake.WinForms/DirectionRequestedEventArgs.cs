using Snake.Application._SetSnakeDirectionUseCase.Dto;

namespace Snake.WinForms
{
    public class DirectionRequestedEventArgs : EventArgs
    {
        public DirectionRequestedEventArgs(SnakeDirectionDTO direction)
        {
            Direction = direction ?? throw new ArgumentNullException(nameof(direction));
        }

        public SnakeDirectionDTO Direction { get; }
    }
}
