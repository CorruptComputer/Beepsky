using Serilog;

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
    public static async Task AwaitWithTimeout<TSource>(this Task<TSource> task, TimeSpan timeout, Func<TSource, Task>? onSuccess = null, Func<Task>? onTimeout = null, Func<Task>? onComplete = null, CancellationTokenSource? tasksLinkedCts = null)
    {
        if (await Task.WhenAny(task, Task.Delay(timeout), WaitTillCancelled(tasksLinkedCts?.Token ?? default)) == task)
        {
            if (onSuccess is not null)
            {
                try
                {
                    // woo hoo
                    await onSuccess(await task);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error in onSuccess callback of AwaitWithTimeout");
                    throw;
                }
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
                try
                {
                    // Probably handle it gracefully
                    await onTimeout();
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error in onTimeout callback of AwaitWithTimeout");
                    throw;
                }
            }
            else
            {
                // yeet
                throw new OperationCanceledException();
            }
        }

        if (onComplete is not null)
        {
            try
            {
                // In places where things need to be disposed, this is nice
                await onComplete();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error in onComplete callback of AwaitWithTimeout");
                throw;
            }
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
    /// <param name="tasksLinkedCts">If provided, this will be cancelled on timeout.</param>
    /// <returns></returns>
    /// <exception cref="OperationCanceledException"></exception>
    public static async Task AwaitWithTimeout(this Task task, TimeSpan timeout, Func<Task>? onSuccess = null, Func<Task>? onTimeout = null, Func<Task>? onComplete = null, CancellationTokenSource? tasksLinkedCts = null)
    {
        if (await Task.WhenAny(task, Task.Delay(timeout), WaitTillCancelled(tasksLinkedCts?.Token ?? default)) == task)
        {
            if (onSuccess is not null)
            {
                try
                {
                    // woo hoo
                    await onSuccess();
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error in onSuccess callback of AwaitWithTimeout");
                    throw;
                }
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
                try
                {
                    // Probably handle it gracefully
                    await onTimeout();
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error in onTimeout callback of AwaitWithTimeout");
                    throw;
                }
            }
            else
            {
                // yeet
                throw new OperationCanceledException();
            }
        }

        if (onComplete is not null)
        {
            try
            {
                // In places where things need to be disposed, this is nice
                await onComplete();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error in onComplete callback of AwaitWithTimeout");
                throw;
            }
        }
    }

    private static async Task WaitTillCancelled(CancellationToken cancellationToken)
    {
        if (!cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
        }
    }
}