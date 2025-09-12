namespace CX.Engine.Common;

public static class CxTask
{
    /// <summary>
    /// Calls Task.WhenAll - but allows a null enumeration.
    /// </summary>
    public static Task WhenAll(IEnumerable<Task> tasks)
    {
        if (tasks == null) 
            return Task.CompletedTask;
        
        return Task.WhenAll(tasks);
    }
}