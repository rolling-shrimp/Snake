using System.Runtime.ExceptionServices;

namespace Snake.Test;

public class StaTestRunner
{
    public T Run<T>(Func<T> action)
    {
        T? result = default;
        Exception? failure = null;
        Thread thread = new Thread(() =>
        {
            try
            {
                result = action();
            }
            catch (Exception exception)
            {
                failure = exception;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        if (failure is not null)
        {
            ExceptionDispatchInfo.Capture(failure).Throw();
        }

        return result!;
    }
}
