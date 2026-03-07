using System.Diagnostics;
using Serilog;
using Serilog.Events;

namespace Beepsky.PipelineBehaviors;

/// <summary>
///   Base pipeline behavior for Questy
/// </summary>
/// <typeparam name="TRequest"></typeparam>
/// <typeparam name="TResponse"></typeparam>
public abstract class PipelineBehaviorBase<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    /// <summary>
    ///   Gets the basic result info from the response
    /// </summary>
    /// <param name="response"></param>
    /// <returns></returns>
    protected abstract bool GetResult(TResponse response);

    /// <summary>
    ///   Generate a failure response with the provided type
    /// </summary>
    /// <returns></returns>
    protected abstract TResponse GetGenericFailedResponse();

    private readonly bool _debugLog = Log.IsEnabled(LogEventLevel.Debug);

    private Stopwatch? _stopwatch;

    /// <summary>
    ///   This is called before Questy sends the request to its handler, we pass the request along with next()
    /// </summary>
    /// <param name="request"></param>
    /// <param name="next"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        StartDebugLog(request);
        Exception? exception = null;
        TResponse? response;

        try
        {
            response = await next(cancellationToken);
        }
        catch (Exception e)
        {
            Log.Error(e, "Uncaught Exception [{RequestName}] | ExceptionMessage = {Message}", typeof(TRequest).FullName, e.Message);

            exception = e;
            response = GetGenericFailedResponse();
        }

        bool success = GetResult(response);

        StopDebugLog(request, success, exception);

        return response;
    }

    private void StartDebugLog(TRequest request)
    {
        if (!_debugLog)
        {
            return;
        }

        Log.Debug("Started [{TypeName}] TRequest = {RequestBody}", typeof(TRequest).FullName, request.ToString());
        _stopwatch = Stopwatch.StartNew();
    }

    private void StopDebugLog(TRequest request, bool success, Exception? exception)
    {
        if (!_debugLog || _stopwatch is null)
        {
            return;
        }

        _stopwatch.Stop();
        if (exception is not null)
        {
            Log.Debug("Uncaught Exception [{TypeName}] | Exception = {ExceptionMesssage} | TRequest = {RequestBody}", typeof(TRequest).FullName, exception.Message, request.ToString());
            if (Debugger.IsAttached)
            {
                // oops!
                Debugger.Break();
            }
        }

        if (success)
        {
            Log.Debug("Succeeded [{TypeName}] | TRequest = {RequestBody}", typeof(TRequest).FullName, request.ToString());
        }
        else
        {
            Log.Debug("Failed [{TypeName}] | TRequest = {RequestBody}", typeof(TRequest).FullName, request.ToString());
        }
    }
}