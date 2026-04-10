using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        _dispatcher = CreateDispatcher();
    }

    /// <summary>
    /// Determines whether an incoming level should be accepted.
    /// </summary>
    public bool IsEnabled(LogLevel level)
    {
        if (_disposed || _options.LoggerType == LogDispatchBehavior.Silent)
        {
            return false;
        }

        return level != LogLevel.None && level >= _options.MinLevel;
    }

    /// <summary>
    /// Writes a fully described event to the configured dispatcher.
    /// </summary>
    public void Write(
        string category,
        LogLevel level,
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
            _options.TimestampProvider(),
            level,
            category,
            eventId,
            message,
            exception,
            scopes ?? [],
            resolvedCaller);

        string fileRendered = _formatter.FormatFile(entry);
        _dispatcher.Enqueue(entry, fileRendered);
        if (_options.LoggerType != LogDispatchBehavior.Async)
        {
            WriteConsole(entry);
            PublishEvent(entry);
        }
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

    private IObservableLogDispatcher CreateDispatcher()
    {
        if (_options.LoggerType == LogDispatchBehavior.Silent)
        {
            return new NoneLogDispatcher();
        }

        ObservableFileWriter writer = new(_options);
        return _options.LoggerType == LogDispatchBehavior.Sync
            ? new SyncLogDispatcher(writer)
            : new AsyncLogDispatcher(writer, DispatchSecondaryTargets);
    }

    private void DispatchSecondaryTargets(LogEntry entry)
    {
        WriteConsole(entry);
        PublishEvent(entry);
    }

    private void WriteConsole(LogEntry entry)
    {
        if (!_options.WriteToConsole)
        {
            return;
        }

        string rendered = _formatter.FormatConsole(entry);
        if (_options.ConsoleTarget == ConsoleTarget.Debug)
        {
            Debug.WriteLine(rendered);
            return;
        }

        Console.WriteLine(rendered);
    }

    private void PublishEvent(LogEntry entry)
    {
        if (!_options.PublishToEvent)
        {
            return;
        }

        string rendered = _formatter.FormatEvent(entry);

        Log.Publish(new ObservableLogEvent(
            entry.Timestamp,
            entry.Level,
            entry.Category,
            entry.Message,
            entry.Exception,
            rendered));
    }
}
