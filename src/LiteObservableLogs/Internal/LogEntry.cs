using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace LiteObservableLogs.Internal;

/// <summary>
/// Immutable snapshot of one log event after filtering and enrichment, ready for formatting and I/O.
/// </summary>
internal sealed class LogEntry
{
    /// <summary>
    /// Initializes a new log entry with all fields materialized for the sink pipeline.
    /// </summary>
    public LogEntry(
        DateTimeOffset timestamp,
        LogLevel level,
        string category,
        EventId eventId,
        string message,
        Exception? exception,
        IReadOnlyList<string> scopes,
        CallerInfo? caller)
    {
        Timestamp = timestamp;
        Level = level;
        Category = category;
        EventId = eventId;
        Message = message;
        Exception = exception;
        Scopes = scopes;
        Caller = caller;
    }

    /// <summary>Event time from <see cref="ObservableLoggerOptions.TimestampProvider"/>.</summary>
    public DateTimeOffset Timestamp { get; }

    /// <summary>Microsoft log level for this entry.</summary>
    public LogLevel Level { get; }

    /// <summary>Logger category (often source context or type name).</summary>
    public string Category { get; }

    /// <summary>Optional structured event identifier from the logging API.</summary>
    public EventId EventId { get; }

    /// <summary>Rendered message text.</summary>
    public string Message { get; }

    /// <summary>Exception attached to the log call, if any.</summary>
    public Exception? Exception { get; }

    /// <summary>Outer-to-inner scope labels captured at log time.</summary>
    public IReadOnlyList<string> Scopes { get; }

    /// <summary>Optional caller file, line, member, and thread metadata.</summary>
    public CallerInfo? Caller { get; }
}
