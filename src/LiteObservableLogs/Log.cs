using System;
using System.Threading;

namespace LiteObservableLogs;

/// <summary>
/// Global static entry point for quick logging without passing logger instances around.
/// </summary>
public static class Log
{
    /// <summary>
    /// Gets a no-op facade used as a safe fallback when no logger has been configured.
    /// </summary>
    public static ObservableLoggerFacade Empty { get; } = ObservableLoggerFacade.Empty;

    private static ObservableLoggerFacade _logger = Empty;
    private static ObservableLoggerOptions _currentOptions = new();

    /// <summary>
    /// Raised when event publishing is enabled and a log entry is written.
    /// </summary>
    public static event EventHandler<ObservableLogEvent>? Received;

    /// <summary>
    /// Gets or sets the current global logger facade.
    /// </summary>
    /// <remarks>
    /// A <c>null</c> assignment is normalized to <see cref="Empty"/> to avoid null checks.
    /// </remarks>
    public static ObservableLoggerFacade Logger
    {
        get => _logger;
        set
        {
            ObservableLoggerFacade next = value ?? Empty;
            _logger = next;
            _currentOptions = next.OptionsSnapshot ?? new ObservableLoggerOptions();
        }
    }

    /// <summary>
    /// Gets the latest known options from the current global logger.
    /// </summary>
    internal static ObservableLoggerOptions CurrentOptions => _currentOptions.Clone();

    /// <summary>
    /// Flushes and disposes the current global logger, then resets it to <see cref="Empty"/>.
    /// </summary>
    public static void CloseAndFlush()
    {
        ObservableLoggerFacade logger = Interlocked.Exchange(ref _logger, Empty);
        _currentOptions = new ObservableLoggerOptions();
        logger.Flush();
        logger.Dispose();
    }

    /// <summary>
    /// Emits a no-op log call for API symmetry.
    /// </summary>
    public static void None(params object[] values)
    {
        _logger.None(values);
    }

    /// <summary>
    /// Writes a trace log entry through the global logger.
    /// </summary>
    public static void Trace(params object[] values)
    {
        _logger.Trace(values);
    }

    /// <summary>
    /// Writes a debug log entry through the global logger.
    /// </summary>
    public static void Debug(params object[] values)
    {
        _logger.Debug(values);
    }

    /// <summary>
    /// Writes an information log entry through the global logger.
    /// </summary>
    public static void Information(params object[] values)
    {
        _logger.Information(values);
    }

    /// <summary>
    /// Writes a warning log entry through the global logger.
    /// </summary>
    public static void Warning(params object[] values)
    {
        _logger.Warning(values);
    }

    /// <summary>
    /// Writes an error log entry through the global logger.
    /// </summary>
    public static void Error(params object[] values)
    {
        _logger.Error(values);
    }

    /// <summary>
    /// Writes a critical log entry through the global logger.
    /// </summary>
    public static void Critical(params object[] values)
    {
        _logger.Critical(values);
    }

    /// <summary>
    /// Writes an exception as an error log entry with an optional custom message.
    /// </summary>
    public static void Exception(Exception exception, string? message = null)
    {
        _logger.Exception(exception, message);
    }

    /// <summary>
    /// Raises <see cref="Received"/> when event publishing is enabled on the active sink.
    /// </summary>
    internal static void Publish(ObservableLogEvent entry)
    {
        EventHandler<ObservableLogEvent>? handlers = Received;
        if (handlers == null)
        {
            return;
        }

        // Do not call Received?.Invoke(null, entry) directly:
        // multicast delegate invocation stops on the first throwing subscriber.
        // We invoke each handler individually so one faulty listener does not
        // block other listeners or destabilize the logging pipeline.
        Delegate[] invocationList = handlers.GetInvocationList();
        for (int i = 0; i < invocationList.Length; i++)
        {
            try
            {
                ((EventHandler<ObservableLogEvent>)invocationList[i]).Invoke(null, entry);
            }
            catch (Exception ex)
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine($"[LiteObservableLogs] Log.Received subscriber threw: {ex}");
#endif
            }
        }
    }
}
