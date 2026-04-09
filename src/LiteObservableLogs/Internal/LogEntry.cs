using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace LiteObservableLogs.Internal;

internal sealed class LogEntry
{
    public LogEntry(
        DateTimeOffset timestamp,
        Microsoft.Extensions.Logging.LogLevel level,
        string category,
        EventId eventId,
        string message,
        Exception? exception,
        IReadOnlyList<string> scopes,
        CallerInfo? caller)
    {
        Timestamp = timestamp;
        Level = (LogLevel)level;
        Category = category;
        EventId = eventId;
        Message = message;
        Exception = exception;
        Scopes = scopes;
        Caller = caller;
    }

    public DateTimeOffset Timestamp { get; }

    public LogLevel Level { get; }

    public string Category { get; }

    public EventId EventId { get; }

    public string Message { get; }

    public Exception? Exception { get; }

    public IReadOnlyList<string> Scopes { get; }

    public CallerInfo? Caller { get; }
}
