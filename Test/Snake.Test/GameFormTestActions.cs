using System.Reflection;
using Snake.WinForms;

namespace Snake.Test
{
    public class GameFormTestActions
    {
        public void Start(GameForm form)
        {
            InvokeEventHandler(form, "OnStartButtonClick");
        }

        public void Tick(GameForm form)
        {
            InvokeEventHandler(form, "OnGameTimerTick");
        }

        private void InvokeEventHandler(GameForm form, string methodName)
        {
            MethodInfo method = typeof(GameForm).GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException($"找不到 GameForm.{methodName}。");
            method.Invoke(form, new object?[] { null, EventArgs.Empty });
        }
    }
}
