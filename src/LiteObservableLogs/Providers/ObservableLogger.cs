using System;
using System.Collections.Generic;
using LiteObservableLogs.Internal;
using Microsoft.Extensions.Logging;

namespace LiteObservableLogs.Providers;

/// <summary>
/// Concrete <see cref="ILogger"/> that converts log calls into sink entries.
/// </summary>
public sealed class ObservableLogger : ILogger
{
    private readonly string _categoryName;
    private readonly ObservableLogSink _sink;
    private readonly Func<IExternalScopeProvider?> _scopeProviderAccessor;
    private readonly bool _includeScopes;

    /// <summary>
    /// Binds a category name to the shared sink and optional external scope provider.
    /// </summary>
    internal ObservableLogger(
        string categoryName,
        ObservableLogSink sink,
        Func<IExternalScopeProvider?> scopeProviderAccessor,
        bool includeScopes)
    {
        _categoryName = categoryName;
        _sink = sink;
        _scopeProviderAccessor = scopeProviderAccessor;
        _includeScopes = includeScopes;
    }

    /// <summary>
    /// Begins a scope if scope capture is enabled; otherwise returns a no-op scope.
    /// </summary>
    public IDisposable BeginScope<TState>(TState state)
        where TState : notnull
    {
        if (!_includeScopes)
        {
            return NoneScope.Instance;
        }

        IExternalScopeProvider? scopeProvider = _scopeProviderAccessor();
        return scopeProvider?.Push(state) ?? NoneScope.Instance;
    }

    /// <summary>
    /// Checks whether a level is currently enabled for this sink.
    /// </summary>
    public bool IsEnabled(LogLevel logLevel)
    {
        return _sink.IsEnabled(logLevel);
    }

    /// <summary>
    /// Formats incoming state and forwards a structured entry to the sink.
    /// </summary>
    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }

        string message = formatter == null
            ? state?.ToString() ?? string.Empty
            : formatter(state, exception);

        List<string> scopes = [];
        if (_includeScopes)
        {
            // Snapshot scopes now to keep asynchronous dispatch independent of ambient context.
            _scopeProviderAccessor()?.ForEachScope(static (scope, values) =>
            {
                if (scope != null)
                {
                    values.Add(scope.ToString() ?? string.Empty);
                }
            }, scopes);
        }

        _sink.Write(_categoryName, logLevel, eventId, message, exception, scopes);
    }
}
