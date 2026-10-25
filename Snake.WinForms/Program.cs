using Snake.UseCases;
using Snake.WinForms;

ApplicationConfiguration.Initialize();

GameForm gameForm = new GameForm();
SnakeGameSession session = new SnakeGameSession();
GamePresenter gamePresenter = new GamePresenter(gameForm, session);
gamePresenter.Initialize();
Application.Run(gameForm);
