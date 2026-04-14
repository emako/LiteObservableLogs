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

    /// <summary>Gets the configured log folder.</summary>
    public string LogFolder => _options.LogFolder;

    /// <summary>Gets the current active log file path, or <c>null</c> before first write.</summary>
    public string? CurrentLogFilePath => _dispatcher.CurrentLogFilePath;

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

        if (_options.LoggerType == LogDispatchBehavior.Async)
        {
            // In async mode, keep caller-thread work minimal: only collect snapshot data,
            // enqueue, and let the background worker perform all formatting/output steps.
            _dispatcher.Enqueue(entry, string.Empty, null, null);
            return;
        }

        (string fileRendered, string? consoleRendered, string? eventRendered) = RenderOutputs(entry);
        _dispatcher.Enqueue(entry, fileRendered, consoleRendered, eventRendered);
        WriteConsole(entry, consoleRendered);
        PublishEvent(entry, eventRendered);
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

    /// <summary>Selects silent, synchronous, or asynchronous dispatch and wires secondary-output callbacks.</summary>
    private IObservableLogDispatcher CreateDispatcher()
    {
        if (_options.LoggerType == LogDispatchBehavior.Silent)
        {
            return new NoneLogDispatcher();
        }

        ObservableFileWriter writer = new(_options);
        return _options.LoggerType == LogDispatchBehavior.Sync
            ? new SyncLogDispatcher(writer)
            : new AsyncLogDispatcher(writer, RenderOutputs, DispatchSecondaryTargets);
    }

    /// <summary>Renders sink outputs for file/console/event with one shared token builder.</summary>
    private (string FileRendered, string? ConsoleRendered, string? EventRendered) RenderOutputs(LogEntry entry)
    {
        LogStringBuilder? sharedBuilder = _formatter.CreateSharedBuilder(entry);
        string fileRendered = _formatter.FormatFile(entry, sharedBuilder);
        string? consoleRendered = _options.WriteToConsole
            ? _formatter.FormatConsole(entry, sharedBuilder)
            : null;
        string? eventRendered = _options.PublishToEvent
            ? _formatter.FormatEvent(entry, sharedBuilder)
            : null;
        return (fileRendered, consoleRendered, eventRendered);
    }

    /// <summary>Invoked after async file writes to fan out to console and in-process events.</summary>
    private void DispatchSecondaryTargets(LogEntry entry, string? consoleRendered, string? eventRendered)
    {
        WriteConsole(entry, consoleRendered);
        PublishEvent(entry, eventRendered);
    }

    /// <summary>Renders and writes to <see cref="Console"/> or <see cref="System.Diagnostics.Debug"/> when enabled.</summary>
    private void WriteConsole(LogEntry entry, string? rendered = null)
    {
        if (!_options.WriteToConsole)
        {
            return;
        }

        rendered ??= _formatter.FormatConsole(entry);
        if (_options.ConsoleTarget == ConsoleTarget.Debug)
        {
            Debug.WriteLine(rendered);
            return;
        }

        Console.WriteLine(rendered);
    }

    /// <summary>Raises <see cref="Log.Publish"/> with the event-template formatted line.</summary>
    private void PublishEvent(LogEntry entry, string? rendered = null)
    {
        if (!_options.PublishToEvent)
        {
            return;
        }

        rendered ??= _formatter.FormatEvent(entry);

        Log.Publish(new ObservableLogEvent(
            entry.Timestamp,
            entry.Level,
            entry.Category,
            entry.Message,
            entry.Exception,
            rendered));
    }
}
