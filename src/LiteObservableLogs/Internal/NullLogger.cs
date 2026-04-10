using System;
using Microsoft.Extensions.Logging;

namespace LiteObservableLogs.Internal;

internal sealed class NullLogger : ILogger
{
    public static NullLogger Instance { get; } = new();

    private NullLogger()
    {
    }

    public IDisposable BeginScope<TState>(TState state)
        where TState : notnull
    {
        return NullScope.Instance;
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return false;
    }

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
    }
}
