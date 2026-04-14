using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace LiteObservableLogs.Internal;

/// <summary>
/// Immutable snapshot of one log event after filtering and enrichment, ready for formatting and I/O.
/// </summary>
/// <remarks>
/// Initializes a new log entry with all fields materialized for the sink pipeline.
/// </remarks>
internal sealed class LogEntry(
    DateTimeOffset timestamp,
    LogLevel level,
    string category,
    EventId eventId,
    string message,
    Exception? exception,
    IReadOnlyList<string> scopes,
    CallerInfo? caller,
    string? stackFrames)
{
    /// <summary>Event time from <see cref="ObservableLoggerOptions.TimestampProvider"/>.</summary>
    public DateTimeOffset Timestamp { get; } = timestamp;

    /// <summary>Microsoft log level for this entry.</summary>
    public LogLevel Level { get; } = level;

    /// <summary>Logger category (often source context or type name).</summary>
    public string Category { get; } = category;

    /// <summary>Optional structured event identifier from the logging API.</summary>
    public EventId EventId { get; } = eventId;

    /// <summary>Rendered message text.</summary>
    public string Message { get; } = message;

    /// <summary>Exception attached to the log call, if any.</summary>
    public Exception? Exception { get; } = exception;

    /// <summary>Outer-to-inner scope labels captured at log time.</summary>
    public IReadOnlyList<string> Scopes { get; } = scopes;

    /// <summary>Optional caller file, line, member, and thread metadata.</summary>
    public CallerInfo? Caller { get; } = caller;

    /// <summary>
    /// Stack trace captured at log-call time (outside this library), used by <c>{StackFrames}</c>.
    /// </summary>
    public string StackFrames { get; } = stackFrames ?? string.Empty;
}
