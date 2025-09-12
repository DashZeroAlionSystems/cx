namespace CX.Engine.Common;

public class TaskX
{
    /// <summary>
    /// Loops until condition becomes true.
    /// Default interval is 200ms.
    /// </summary>
    public static Task Until(Func<bool> condition, TimeSpan interval = default)
    {
        if (interval == default)
            interval = TimeSpan.FromMilliseconds(200);

        return Task.Run(async () =>
        {
            while (!condition())
            {
                await Task.Delay(interval);
            }
        });
    }
    
    public static Task Until(Func<Task<bool>> condition, TimeSpan interval = default)
    {
        if (interval == default)
            interval = TimeSpan.FromMilliseconds(200);

        return Task.Run(async () =>
        {
            while (!await condition())
            {
                await Task.Delay(interval);
            }
        });
    }

}