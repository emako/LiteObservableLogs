using System;
using System.Linq;
using LiteObservableLogs.Internal;
using LiteObservableLogs.Providers;
using Microsoft.Extensions.Logging;

namespace LiteObservableLogs;

/// <summary>
/// Lightweight convenience wrapper around <see cref="ILogger"/> with simple object-array APIs.
/// </summary>
public sealed class ObservableLoggerFacade : IDisposable
{
    private readonly ILogger _logger;
    private readonly ObservableLoggerProvider? _provider;
    private readonly bool _ownsProvider;
    private readonly ObservableLoggerOptions? _optionsSnapshot;
    private bool _disposed;

    /// <summary>
    /// Constructs a facade over an existing <see cref="ILogger"/>, optionally owning a provider for flush/dispose.
    /// </summary>
    internal ObservableLoggerFacade(
        ILogger logger,
        ObservableLoggerProvider? provider,
        bool ownsProvider,
        ObservableLoggerOptions? optionsSnapshot = null)
    {
        _logger = logger;
        _provider = provider;
        _ownsProvider = ownsProvider;
        _optionsSnapshot = optionsSnapshot?.Clone();
    }

    /// <summary>
    /// Gets a no-op facade that safely ignores all writes.
    /// </summary>
    public static ObservableLoggerFacade Empty { get; } = new(NoneLogger.Instance, null, false);

    /// <summary>
    /// Gets the underlying Microsoft logger instance.
    /// </summary>
    public ILogger InnerLogger => _logger;

    /// <summary>
    /// Cloned options used when bridging to host logging (for example Serilog-style compatibility).
    /// </summary>
    internal ObservableLoggerOptions? OptionsSnapshot => _optionsSnapshot?.Clone();

    /// <summary>
    /// Gets the configured log folder for this logger instance.
    /// </summary>
    public string? LogFolder
    {
        get
        {
            ThrowIfDisposed();
            return _provider?.LogFolder ?? _optionsSnapshot?.LogFolder;
        }
    }

    /// <summary>
    /// Gets the current active log file path, or <c>null</c> before the first write.
    /// </summary>
    public string? CurrentLogFilePath
    {
        get
        {
            ThrowIfDisposed();
            return _provider?.CurrentLogFilePath;
        }
    }

    /// <summary>
    /// Emits no output. Kept for level API parity.
    /// </summary>
    public void None(params object[] values)
    {
        // Avoid unused parameter warning.
        _ = values;
    }

    /// <summary>
    /// Writes a trace entry.
    /// </summary>
    public void Trace(params object[] values)
    {
        Write(LogLevel.Trace, values);
    }

    /// <summary>
    /// Writes a debug entry.
    /// </summary>
    public void Debug(params object[] values)
    {
        Write(LogLevel.Debug, values);
    }

    /// <summary>
    /// Writes an information entry.
    /// </summary>
    public void Information(params object[] values)
    {
        Write(LogLevel.Information, values);
    }

    /// <summary>
    /// Writes a warning entry.
    /// </summary>
    public void Warning(params object[] values)
    {
        Write(LogLevel.Warning, values);
    }

    /// <summary>
    /// Writes an error entry.
    /// </summary>
    public void Error(params object[] values)
    {
        Write(LogLevel.Error, values);
    }

    /// <summary>
    /// Writes a critical entry.
    /// </summary>
    public void Critical(params object[] values)
    {
        Write(LogLevel.Critical, values);
    }

    /// <summary>
    /// Writes exception details at error level.
    /// </summary>
    public void Exception(Exception exception, string? message = null)
    {
        ThrowIfDisposed();

        string finalMessage = string.IsNullOrWhiteSpace(message)
            ? exception.Message
            : message!;

        _logger.Log(LogLevel.Error, default, finalMessage, exception, static (state, _) => state);
    }

    /// <summary>
    /// Forces pending buffered entries to be written.
    /// </summary>
    public void Flush()
    {
        ThrowIfDisposed();
        _provider?.Flush();
    }

    /// <summary>
    /// Removes a callback previously added via <c>ObserveTo.Callback(...)</c>.
    /// </summary>
    public bool RemoveCallback(Action<ObservableLogEvent> callback)
    {
        ThrowIfDisposed();
        if (callback == null)
        {
            throw new ArgumentNullException(nameof(callback));
        }

        return _provider?.RemoveCallback(callback) ?? false;
    }

    /// <summary>
    /// Disposes the facade and optionally its owned provider.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        if (_ownsProvider)
        {
            _provider?.Dispose();
        }
    }

    private void Write(LogLevel level, params object[] values)
    {
        ThrowIfDisposed();
        // Keep the facade API intentionally simple: object values are joined by spaces.
        string message = string.Join(" ", values.Select(static item => item?.ToString() ?? string.Empty));
        _logger.Log(level, default, message, null, static (state, _) => state);
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(ObservableLoggerFacade));
        }
    }
}
