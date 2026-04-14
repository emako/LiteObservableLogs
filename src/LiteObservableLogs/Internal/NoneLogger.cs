using System;
using Microsoft.Extensions.Logging;

namespace LiteObservableLogs.Internal;

/// <summary>
/// Singleton <see cref="ILogger"/> that discards all output; used by <see cref="ObservableLoggerFacade.Empty"/>.
/// </summary>
internal sealed class NoneLogger : ILogger
{
    /// <summary>Shared instance for no-op logging.</summary>
    public static NoneLogger Instance { get; } = new();

    private NoneLogger()
    {
    }

    /// <inheritdoc />
    public IDisposable BeginScope<TState>(TState state)
        where TState : notnull
    {
        return NoneScope.Instance;
    }

    /// <inheritdoc />
    public bool IsEnabled(LogLevel logLevel)
    {
        return false;
    }

    /// <inheritdoc />
    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
    }
}
