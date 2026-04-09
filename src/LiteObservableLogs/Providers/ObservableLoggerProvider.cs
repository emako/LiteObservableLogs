using System;
using System.Collections.Concurrent;
using LiteObservableLogs.Internal;
using Microsoft.Extensions.Logging;

namespace LiteObservableLogs.Providers;

/// <summary>
/// Microsoft logger provider that owns sink lifetime and category-specific logger caching.
/// </summary>
public sealed class ObservableLoggerProvider : ILoggerProvider, ISupportExternalScope
{
    private readonly ConcurrentDictionary<string, ObservableLogger> _loggers = new(StringComparer.Ordinal);
    private readonly ObservableLogSink _sink;
    private readonly ObservableLoggerOptions _options;

    private IExternalScopeProvider _scopeProvider = new LoggerExternalScopeProvider();

    /// <summary>
    /// Creates a provider with cloned options to isolate caller mutations.
    /// </summary>
    public ObservableLoggerProvider(ObservableLoggerOptions options)
    {
        _options = options.Clone();
        _sink = new ObservableLogSink(_options);
    }

    /// <summary>
    /// Creates or reuses a logger for the requested category.
    /// </summary>
    public ILogger CreateLogger(string categoryName)
    {
        string effectiveCategory = string.IsNullOrWhiteSpace(categoryName)
            ? _options.DefaultCategoryName
            : categoryName;

        return _loggers.GetOrAdd(
            effectiveCategory,
            category => new ObservableLogger(category, _sink, () => _scopeProvider, _options.IncludeScopes));
    }

    /// <summary>
    /// Injects the external scope provider from logging infrastructure.
    /// </summary>
    public void SetScopeProvider(IExternalScopeProvider scopeProvider)
    {
        _scopeProvider = scopeProvider ?? new LoggerExternalScopeProvider();
    }

    /// <summary>
    /// Flushes pending entries from sink dispatchers.
    /// </summary>
    public void Flush()
    {
        _sink.Flush();
    }

    /// <summary>
    /// Disposes sink resources and clears cached logger instances.
    /// </summary>
    public void Dispose()
    {
        _sink.Dispose();
        _loggers.Clear();
    }
}
