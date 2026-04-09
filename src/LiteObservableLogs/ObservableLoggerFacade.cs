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
    private bool _disposed;

    internal ObservableLoggerFacade(ILogger logger, ObservableLoggerProvider? provider, bool ownsProvider)
    {
        _logger = logger;
        _provider = provider;
        _ownsProvider = ownsProvider;
    }

    /// <summary>
    /// Gets a no-op facade that safely ignores all writes.
    /// </summary>
    public static ObservableLoggerFacade Empty { get; } = new(NullLogger.Instance, null, false);

    /// <summary>
    /// Gets the underlying Microsoft logger instance.
    /// </summary>
    public ILogger InnerLogger => _logger;

    /// <summary>
    /// Emits no output. Kept for level API parity.
    /// </summary>
    public void None(params object[] values)
    {
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

        _logger.Log(LogLevel.Error.ToMicrosoft(), default, finalMessage, exception, static (state, _) => state);
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
        _logger.Log(level.ToMicrosoft(), default, message, null, static (state, _) => state);
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(ObservableLoggerFacade));
        }
    }
}
