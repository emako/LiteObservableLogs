using System;
using Microsoft.Extensions.Logging;

namespace LiteObservableLogs;

/// <summary>
/// Represents one observable log entry published through <see cref="Log.Received"/>.
/// </summary>
/// <param name="timestamp">Time of the log event.</param>
/// <param name="level">Severity level.</param>
/// <param name="category">Logger category name.</param>
/// <param name="message">Raw message text before template rendering.</param>
/// <param name="exception">Exception passed to the logger, if any.</param>
/// <param name="renderedText">Final line produced by the configured event output template (or fallback).</param>
public sealed class ObservableLogEvent(
    DateTimeOffset timestamp,
    LogLevel level,
    string category,
    string message,
    Exception? exception,
    string renderedText)
{
    /// <summary>
    /// Time of the log event.
    /// </summary>
    public DateTimeOffset Timestamp { get; } = timestamp;

    /// <summary>
    /// Severity level.
    /// </summary>
    public LogLevel Level { get; } = level;

    /// <summary>
    /// Logger category name.
    /// </summary>
    public string Category { get; } = category;

    /// <summary>
    /// Raw message text before per-sink formatting.
    /// </summary>
    public string Message { get; } = message;

    /// <summary>
    /// Exception passed to the logger, if any.
    /// </summary>
    public Exception? Exception { get; } = exception;

    /// <summary>
    /// Fully formatted line as it would appear for the event sink (console/file templates differ).
    /// </summary>
    public string RenderedText { get; } = renderedText;

    public override string ToString() => RenderedText;
}
