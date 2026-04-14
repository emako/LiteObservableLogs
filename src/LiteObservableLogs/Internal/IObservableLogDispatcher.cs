using System;

namespace LiteObservableLogs.Internal;

/// <summary>
/// Abstraction over sync/async delivery of formatted lines to storage (and optional post-write hooks).
/// </summary>
internal interface IObservableLogDispatcher : IDisposable
{
    /// <summary>
    /// Gets the active log file path currently used by this dispatcher, or <c>null</c> when unavailable.
    /// </summary>
    public string? CurrentLogFilePath { get; }

    /// <summary>
    /// Queues or writes one formatted log line for the given entry.
    /// </summary>
    public void Enqueue(LogEntry entry, string fileMessage, string? consoleMessage, string? eventMessage);

    /// <summary>
    /// Ensures all accepted entries are persisted to the underlying writer.
    /// </summary>
    public void Flush();
}
