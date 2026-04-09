using System;
using LiteObservableLogs.Providers;
using Microsoft.Extensions.Logging;

namespace LiteObservableLogs;

/// <summary>
/// Fluent builder used to configure and create logging primitives.
/// </summary>
public sealed class LoggerConfiguration
{
    private readonly ObservableLoggerOptions _options = new();
    private bool _loggerCreated;

    /// <summary>
    /// Creates a new configuration instance with default option values.
    /// </summary>
    public static LoggerConfiguration CreateDefault()
    {
        return new LoggerConfiguration();
    }

    /// <summary>
    /// Sets how log records are dispatched to storage.
    /// </summary>
    public LoggerConfiguration UseType(LoggerType type = LoggerType.Async)
    {
        _options.LoggerType = type;
        return this;
    }

    /// <summary>
    /// Sets the minimum accepted log level.
    /// </summary>
    public LoggerConfiguration UseLevel(LogLevel level = LogLevel.Trace)
    {
        _options.MinLevel = level;
        return this;
    }

    /// <summary>
    /// Sets log output folder and optional fixed file name.
    /// </summary>
    public LoggerConfiguration WriteToFile(string logFolder, string? fileName = null)
    {
        _options.LogFolder = logFolder;
        _options.FileName = fileName;
        return this;
    }

    /// <summary>
    /// Sets the default category used by facades created from this configuration.
    /// </summary>
    public LoggerConfiguration UseCategory(string categoryName)
    {
        _options.DefaultCategoryName = categoryName;
        return this;
    }

    /// <summary>
    /// Enables or disables scope capture.
    /// </summary>
    public LoggerConfiguration IncludeScopes(bool includeScopes = true)
    {
        _options.IncludeScopes = includeScopes;
        return this;
    }

    /// <summary>
    /// Enables or disables caller metadata capture.
    /// </summary>
    public LoggerConfiguration IncludeCallerInfo(bool includeCallerInfo = true)
    {
        _options.IncludeCallerInfo = includeCallerInfo;
        return this;
    }

    /// <summary>
    /// Applies custom option mutations using a callback.
    /// </summary>
    public LoggerConfiguration UseOptions(Action<ObservableLoggerOptions> configure)
    {
        if (configure == null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        configure(_options);
        return this;
    }

    /// <summary>
    /// Builds a safe cloned options snapshot.
    /// </summary>
    public ObservableLoggerOptions BuildOptions()
    {
        return _options.Clone();
    }

    /// <summary>
    /// Creates a provider for integration with <see cref="ILoggerFactory"/> pipelines.
    /// </summary>
    public ObservableLoggerProvider CreateProvider()
    {
        EnsureNotCreated();
        _loggerCreated = true;
        return new ObservableLoggerProvider(_options);
    }

    /// <summary>
    /// Creates a facade logger using the configured default category.
    /// </summary>
    public ObservableLoggerFacade CreateLogger()
    {
        return CreateLogger(_options.DefaultCategoryName);
    }

    /// <summary>
    /// Creates a facade logger with an explicit category.
    /// </summary>
    public ObservableLoggerFacade CreateLogger(string categoryName)
    {
        EnsureNotCreated();
        _loggerCreated = true;

        ObservableLoggerProvider provider = new(_options);
        ILogger logger = provider.CreateLogger(categoryName);
        return new ObservableLoggerFacade(logger, provider, true);
    }

    /// <summary>
    /// Creates a facade logger that writes synchronously.
    /// </summary>
    public ObservableLoggerFacade CreateSyncLogger(string? categoryName = null)
    {
        _options.LoggerType = LoggerType.Sync;
        return CreateLogger(categoryName ?? _options.DefaultCategoryName);
    }

    /// <summary>
    /// Creates a facade logger that writes asynchronously.
    /// </summary>
    public ObservableLoggerFacade CreateAsyncLogger(string? categoryName = null)
    {
        _options.LoggerType = LoggerType.Async;
        return CreateLogger(categoryName ?? _options.DefaultCategoryName);
    }

    /// <summary>
    /// Creates an <see cref="ILoggerFactory"/> containing this provider.
    /// </summary>
    public ILoggerFactory CreateLoggerFactory()
    {
        EnsureNotCreated();
        _loggerCreated = true;
        ObservableLoggerProvider provider = new(_options);
        return LoggerFactory.Create(builder => builder.AddProvider(provider));
    }

    private void EnsureNotCreated()
    {
        if (_loggerCreated)
        {
            // The builder is intentionally one-shot to avoid accidental shared mutable state.
            throw new InvalidOperationException("Duplicated logger created.");
        }
    }
}
