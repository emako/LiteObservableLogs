using MelLogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace LiteObservableLogs;

/// <summary>
/// Defines severity levels used by this logging package.
/// </summary>
public enum LogLevel
{
    Trace = 0,
    Debug = 1,
    Information = 2,
    Warning = 3,
    Error = 4,
    Critical = 5,
    None = 6,
}

internal static class LogLevelExtensions
{
    /// <summary>
    /// Converts package-level log levels to Microsoft logging levels.
    /// </summary>
    public static MelLogLevel ToMicrosoft(this LogLevel level)
    {
        return level switch
        {
            LogLevel.Trace => MelLogLevel.Trace,
            LogLevel.Debug => MelLogLevel.Debug,
            LogLevel.Information => MelLogLevel.Information,
            LogLevel.Warning => MelLogLevel.Warning,
            LogLevel.Error => MelLogLevel.Error,
            LogLevel.Critical => MelLogLevel.Critical,
            _ => MelLogLevel.None,
        };
    }

    /// <summary>
    /// Converts Microsoft logging levels to package-level log levels.
    /// </summary>
    public static LogLevel ToLiteObservable(this MelLogLevel level)
    {
        return level switch
        {
            MelLogLevel.Trace => LogLevel.Trace,
            MelLogLevel.Debug => LogLevel.Debug,
            MelLogLevel.Information => LogLevel.Information,
            MelLogLevel.Warning => LogLevel.Warning,
            MelLogLevel.Error => LogLevel.Error,
            MelLogLevel.Critical => LogLevel.Critical,
            _ => LogLevel.None,
        };
    }
}
