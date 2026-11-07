using Snake.Application.Repositories;
using Snake.Domain;

namespace Snake.Application._UpdateThirdLevelDoorStateUseCase
{
    public class UpdateThirdLevelDoorStateUseCase
    {
        private const int WARNING_TICK = 16;
        private const int TOGGLE_TICK = 24;

        private Game _game;

        public UpdateThirdLevelDoorStateUseCase(IGameRepository gameRepository)
        {
            ArgumentNullException.ThrowIfNull(gameRepository);
            _game = gameRepository.GetCurrentGame();
        }

        public void UpdateThirdLevelDoorState()
        {
            try
            {
                _game.BeginTick();
                if (_game.Phase != GamePhase.Playing
                    || _game.Level.Number != LevelNumber.Third)
                {
                    return;
                }

                _game.DoorAndMaze.CloseDeferredDoors(_game.Snake.BodyCells);
                _game.DoorTickCount++;
                if (_game.DoorTickCount == WARNING_TICK)
                {
                    _game.DoorAndMaze.ApplyClosingWarning();
                }

                if (_game.DoorTickCount >= TOGGLE_TICK)
                {
                    _game.DoorAndMaze.ApplyToggle(_game.Snake.BodyCells);
                    _game.DoorTickCount = 0;
                }
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException("UpdateThirdLevelDoorState 執行失敗。", exception);
            }
        }
    }
}

