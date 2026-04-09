using System;
using System.Collections.Generic;
using MelLogLevel = Microsoft.Extensions.Logging.LogLevel;
using Microsoft.Extensions.Logging;

namespace LiteObservableLogs.Internal;

internal sealed class LogEntry
{
    public LogEntry(
        DateTimeOffset timestamp,
        MelLogLevel level,
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

    public DateTimeOffset Timestamp { get; }

    public MelLogLevel Level { get; }

    public string Category { get; }

    public EventId EventId { get; }

    public string Message { get; }

    public Exception? Exception { get; }

    public IReadOnlyList<string> Scopes { get; }

    public CallerInfo? Caller { get; }
}
