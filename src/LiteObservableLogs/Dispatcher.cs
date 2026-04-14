namespace LiteObservableLogs;

/// <summary>
/// Specifies dispatch behavior for log writing.
/// </summary>
public enum LogDispatcher
{
    /// <summary>
    /// Drops all entries.
    /// </summary>
    Silent = 0,

    /// <summary>
    /// Buffers entries and writes on a background worker.
    /// </summary>
    Async = 1,

    /// <summary>
    /// Writes entries immediately on the caller thread.
    /// </summary>
    Sync = 2,
}
