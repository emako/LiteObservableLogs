using Microsoft.Extensions.Logging;
using System;

namespace LiteObservableLogs;

/// <summary>
/// Represents one observable log entry published through <see cref="Log.Received"/>.
/// </summary>
public sealed class ObservableLogEvent
{
    public ObservableLogEvent(
        DateTimeOffset timestamp,
        LogLevel level,
        string category,
        string message,
        Exception? exception,
        string renderedText)
    {
        Timestamp = timestamp;
        Level = level;
        Category = category;
        Message = message;
        Exception = exception;
        RenderedText = renderedText;
    }

    public DateTimeOffset Timestamp { get; }

    public LogLevel Level { get; }

    public string Category { get; }

    public string Message { get; }

    public Exception? Exception { get; }

    public string RenderedText { get; }
}
