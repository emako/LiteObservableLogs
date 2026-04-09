using System;
using System.Collections.Generic;
using MelLogLevel = Microsoft.Extensions.Logging.LogLevel;
using Microsoft.Extensions.Logging;

namespace LiteObservableLogs.Internal;

/// <summary>
/// Central sink that filters, enriches, formats, and dispatches log entries.
/// </summary>
internal sealed class ObservableLogSink : IDisposable
{
    private readonly ObservableLoggerOptions _options;
    private readonly ObservableLogFormatter _formatter;
    private readonly IObservableLogDispatcher _dispatcher;
    private bool _disposed;

    public ObservableLogSink(ObservableLoggerOptions options)
    {
        _options = options.Clone();
        _formatter = new ObservableLogFormatter(_options);
        _dispatcher = CreateDispatcher(_options);
    }

    /// <summary>
    /// Determines whether an incoming level should be accepted.
    /// </summary>
    public bool IsEnabled(MelLogLevel level)
    {
        if (_disposed || _options.LoggerType == LoggerType.Silent)
        {
            return false;
        }

        return level != MelLogLevel.None && level >= _options.MinLevel.ToMicrosoft();
    }

    /// <summary>
    /// Writes a fully described event to the configured dispatcher.
    /// </summary>
    public void Write(
        string category,
        MelLogLevel level,
        EventId eventId,
        string message,
        Exception? exception,
        IReadOnlyList<string>? scopes = null,
        CallerInfo? caller = null)
    {
        if (!IsEnabled(level))
        {
            return;
        }

        CallerInfo? resolvedCaller = _options.IncludeCallerInfo ? caller ?? CallerInfoResolver.Resolve() : null;
        // Capture timestamp once so formatted message and file writer stay aligned.
        LogEntry entry = new(
            DateTimeOffset.Now,
            level,
            category,
            eventId,
            message,
            exception,
            scopes ?? Array.Empty<string>(),
            resolvedCaller);

        string rendered = _formatter.Format(entry);
        _dispatcher.Enqueue(entry, rendered);
        WriteConsole(rendered);
        PublishEvent(entry, rendered);
    }

    /// <summary>
    /// Flushes dispatcher buffers if sink is still active.
    /// </summary>
    public void Flush()
    {
        if (_disposed)
        {
            return;
        }

        _dispatcher.Flush();
    }

    /// <summary>
    /// Disposes dispatcher resources and prevents further writes.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _dispatcher.Dispose();
    }

    private static IObservableLogDispatcher CreateDispatcher(ObservableLoggerOptions options)
    {
        if (options.LoggerType == LoggerType.Silent)
        {
            return new NoneLogDispatcher();
        }

        ObservableFileWriter writer = new(options);
        return options.LoggerType == LoggerType.Sync
            ? new SyncLogDispatcher(writer)
            : new AsyncLogDispatcher(writer);
    }

    private void WriteConsole(string rendered)
    {
        if (!_options.WriteToConsole)
        {
            return;
        }

        Console.WriteLine(rendered);
    }

    private void PublishEvent(LogEntry entry, string rendered)
    {
        if (!_options.PublishToEvent)
        {
            return;
        }

        Log.Publish(new ObservableLogEvent(
            entry.Timestamp,
            entry.Level.ToLiteObservable(),
            entry.Category,
            entry.Message,
            entry.Exception,
            rendered));
    }
}
