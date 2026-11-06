namespace Snake.UseCases;

public interface ISnakeGameApplicationService
{
    GetCurrentScreenResult GetCurrentScreen(GetCurrentScreenQuery query);
    StartCurrentLevelResult StartCurrentLevel(StartCurrentLevelCommand command);
    SetSnakeDirectionResult SetSnakeDirection(SetSnakeDirectionCommand command);
    ProcessGameTickResult ProcessGameTick(ProcessGameTickCommand command);
    RestartGameResult RestartGame(RestartGameCommand command);
}
