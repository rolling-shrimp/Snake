using Snake.UseCases;
using Snake.WinForms;

ApplicationConfiguration.Initialize();

GameForm gameForm = new GameForm();
SnakeGameSession session = new SnakeGameSession();
RandomSpawnPositionSelector spawnPositionSelector = new RandomSpawnPositionSelector();
SnakeGameApplicationService applicationService = new SnakeGameApplicationService(
    session,
    spawnPositionSelector);
GamePresenter gamePresenter = new GamePresenter(gameForm, applicationService);
gamePresenter.Initialize();
Application.Run(gameForm);
