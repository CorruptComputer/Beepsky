namespace Beepsky.Extensions;

/// <summary>
///   Task extension methods
/// </summary>
public static class TaskExtensions
{
    /// <summary>
    ///   Awaits a Task&lt;TSource&gt; with a timeout and optional callbacks, if no onTimeout is provided an OperationCanceledException is thrown when the timeout is reached
    /// </summary>
    /// <typeparam name="TSource"></typeparam>
    /// <param name="task"></param>
    /// <param name="timeout"></param>
    /// <param name="onSuccess"></param>
    /// <param name="onTimeout"></param>
    /// <param name="onComplete"></param>
    /// <param name="tasksLinkedCts"></param>
    /// <returns></returns>
    /// <exception cref="OperationCanceledException"></exception>
    public static async Task AwaitWithTimeout<TSource>(this Task<TSource> task, TimeSpan timeout, Action<TSource>? onSuccess = null, Action? onTimeout = null, Action? onComplete = null, CancellationTokenSource? tasksLinkedCts = null)
    {
        if (await Task.WhenAny(task, Task.Delay(timeout), WaitTillCancelled(tasksLinkedCts?.Token ?? default)) == task)
        {
            if (onSuccess is not null)
            {
                // woo hoo
                onSuccess(await task);
            }
        }
        else
        {
            if (tasksLinkedCts is not null)
            {
                // Cancel the task if a linked CTS is provided
                await tasksLinkedCts.CancelAsync();
            }

            if (onTimeout is not null)
            {
                // Probably handle it gracefully
                onTimeout();
            }
            else
            {
                // yeet
                throw new OperationCanceledException();
            }
        }

        if (onComplete is not null)
        {
            // In places where things need to be disposed, this is nice
            onComplete();
        }
    }

    /// <summary>
    ///   Awaits a Task with a timeout and optional callbacks, if no onTimeout is provided an OperationCanceledException is thrown when the timeout is reached
    /// </summary>
    /// <param name="task"></param>
    /// <param name="timeout"></param>
    /// <param name="onSuccess"></param>
    /// <param name="onTimeout"></param>
    /// <param name="onComplete"></param>
    /// <param name="tasksLinkedCts"></param>
    /// <returns></returns>
    /// <exception cref="OperationCanceledException"></exception>
    public static async Task AwaitWithTimeout(this Task task, TimeSpan timeout, Action? onSuccess = null, Action? onTimeout = null, Action? onComplete = null, CancellationTokenSource? tasksLinkedCts = null)
    {
        if (await Task.WhenAny(task, Task.Delay(timeout), WaitTillCancelled(tasksLinkedCts?.Token ?? default)) == task)
        {
            if (onSuccess is not null)
            {
                // woo hoo
                onSuccess();
            }
        }
        else
        {
            if (tasksLinkedCts is not null)
            {
                // Cancel the task if a linked CTS is provided
                await tasksLinkedCts.CancelAsync();
            }

            if (onTimeout is not null)
            {
                // Probably handle it gracefully
                onTimeout();
            }
            else
            {
                // yeet
                throw new OperationCanceledException();
            }
        }

        if (onComplete is not null)
        {
            // In places where things need to be disposed, this is nice
            onComplete();
        }
    }

    private static async Task WaitTillCancelled(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(100, cancellationToken);
        }
    }
}