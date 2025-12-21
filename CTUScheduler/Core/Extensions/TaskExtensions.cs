using System.Threading;
using System.Threading.Tasks;

namespace CTUScheduler.Core.Extensions;

public static class TaskExtensions
{
    /// <summary>
    /// Mark exception as handled
    /// </summary>
    public static void FireAndForgetSafe(this Task task)
    {
        if (task.IsCompletedSuccessfully) return;
        
        task.ContinueWith(t => 
            {
                var _ = t.Exception; 
            }, 
            CancellationToken.None,
            TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);
    }
}