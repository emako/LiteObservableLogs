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
    private readonly WriteToConfiguration _writeTo;
    private readonly MinimumLevelConfiguration _minimumLevel;
    private readonly LogDispatchBehaviorConfiguration _logDispatchBehavior;
    private bool _loggerCreated;

    /// <summary>
    /// Creates a new configuration with default values.
    /// </summary>
    public LoggerConfiguration()
    {
        _writeTo = new WriteToConfiguration(this);
        _minimumLevel = new MinimumLevelConfiguration(this);
        _logDispatchBehavior = new LogDispatchBehaviorConfiguration(this);
    }

    /// <summary>
    /// Creates a new configuration instance with default option values.
    /// </summary>
    public static LoggerConfiguration CreateDefault()
    {
        return new LoggerConfiguration();
    }

    /// <summary>
    /// Provides Serilog-style sink configuration entry.
    /// </summary>
    public WriteToConfiguration WriteTo => _writeTo;

    /// <summary>
    /// Provides Serilog-style minimum level configuration entry.
    /// </summary>
    public MinimumLevelConfiguration MinimumLevel => _minimumLevel;

    /// <summary>
    /// Provides Serilog-style logger type configuration entry.
    /// </summary>
    public LogDispatchBehaviorConfiguration LogDispatchBehavior => _logDispatchBehavior;

    /// <summary>
    /// Sets how log records are dispatched to storage.
    /// </summary>
    public LoggerConfiguration UseDispatchBehavior(LogDispatchBehavior type = LiteObservableLogs.LogDispatchBehavior.Async)
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
        return new ObservableLoggerFacade(logger, provider, true, _options);
    }

    /// <summary>
    /// Creates a facade logger that writes synchronously.
    /// </summary>
    public ObservableLoggerFacade CreateSyncLogger(string? categoryName = null)
    {
        _options.LoggerType = LiteObservableLogs.LogDispatchBehavior.Sync;
        return CreateLogger(categoryName ?? _options.DefaultCategoryName);
    }

    /// <summary>
    /// Creates a facade logger that writes asynchronously.
    /// </summary>
    public ObservableLoggerFacade CreateAsyncLogger(string? categoryName = null)
    {
        _options.LoggerType = LiteObservableLogs.LogDispatchBehavior.Async;
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

    internal LoggerConfiguration SetWriteToFileCompatibility(
        string path,
        string? outputTemplate,
        RollingInterval rollingInterval,
        int? retainedFileCountLimit,
        TimeSpan? retainedFileTimeLimit)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Path cannot be null or whitespace.", nameof(path));
        }

        string fullPath = System.IO.Path.GetFullPath(path);
        string? folder = System.IO.Path.GetDirectoryName(fullPath);
        string fileName = System.IO.Path.GetFileName(fullPath);

        _options.LogFolder = string.IsNullOrWhiteSpace(folder)
            ? AppContext.BaseDirectory
            : folder!;
        _options.FileNameTemplate = fileName;
        _options.RollingInterval = rollingInterval;
        _options.FileName = null;
        _options.FileOutputTemplate = outputTemplate;
        _options.RetainedFileCountLimit = retainedFileCountLimit;
        _options.RetainedFileTimeLimit = retainedFileTimeLimit;

        if (rollingInterval != RollingInterval.Infinite && !_options.FileNameTemplate.Contains("{Timestamp:", StringComparison.Ordinal))
        {
            // Keep compatibility predictable when callers choose rolling interval but omit timestamp placeholder.
            _options.FileNameTemplate = AppendRollingSuffix(_options.FileNameTemplate, rollingInterval);
        }

        return this;
    }

    internal LoggerConfiguration EnableConsoleCompatibility(string? outputTemplate, ConsoleTarget target = ConsoleTarget.Console)
    {
        _options.WriteToConsole = true;
        _options.ConsoleTarget = target;
        _options.ConsoleOutputTemplate = outputTemplate;
        return this;
    }

    internal LoggerConfiguration EnableEventCompatibility(string? outputTemplate)
    {
        _options.PublishToEvent = true;
        _options.EventOutputTemplate = outputTemplate;
        return this;
    }

    internal LoggerConfiguration SetWriteToOptionsCompatibility(string? outputTemplate)
    {
        _options.OutputTemplate = outputTemplate;
        return this;
    }

    private static string AppendRollingSuffix(string fileName, RollingInterval rollingInterval)
    {
        string extension = System.IO.Path.GetExtension(fileName);
        string withoutExtension = extension.Length == 0
            ? fileName
            : fileName.Substring(0, fileName.Length - extension.Length);
        string format = rollingInterval switch
        {
            RollingInterval.Year => "yyyy",
            RollingInterval.Month => "yyyyMM",
            RollingInterval.Day => "yyyyMMdd",
            RollingInterval.Hour => "yyyyMMddHH",
            RollingInterval.Minute => "yyyyMMddHHmm",
            _ => "yyyyMMdd",
        };

        return $"{withoutExtension}_{{Timestamp:{format}}}{extension}";
    }

    /// <summary>
    /// Serilog-style sink configuration compatibility wrapper.
    /// </summary>
    public sealed class WriteToConfiguration
    {
        private readonly LoggerConfiguration _owner;

        internal WriteToConfiguration(LoggerConfiguration owner)
        {
            _owner = owner;
        }

        public LoggerConfiguration File(
            string path,
            string? outputTemplate = null,
            RollingInterval rollingInterval = RollingInterval.Infinite,
            int? retainedFileCountLimit = null,
            TimeSpan? retainedFileTimeLimit = null)
        {
            return _owner.SetWriteToFileCompatibility(path, outputTemplate, rollingInterval, retainedFileCountLimit, retainedFileTimeLimit);
        }

        public LoggerConfiguration Console(string? outputTemplate = null, ConsoleTarget target = ConsoleTarget.Console)
        {
            return _owner.EnableConsoleCompatibility(outputTemplate, target);
        }

        public LoggerConfiguration Event(string? outputTemplate = null)
        {
            return _owner.EnableEventCompatibility(outputTemplate);
        }

        public LoggerConfiguration Option(string? outputTemplate = null)
        {
            return _owner.SetWriteToOptionsCompatibility(outputTemplate);
        }
    }

    /// <summary>
    /// Serilog-style minimum level configuration compatibility wrapper.
    /// </summary>
    public sealed class MinimumLevelConfiguration
    {
        private readonly LoggerConfiguration _owner;

        internal MinimumLevelConfiguration(LoggerConfiguration owner)
        {
            _owner = owner;
        }

        public LoggerConfiguration Verbose()
        {
            return _owner.UseLevel(LogLevel.Trace);
        }

        public LoggerConfiguration Trace()
        {
            return _owner.UseLevel(LogLevel.Trace);
        }

        public LoggerConfiguration Debug()
        {
            return _owner.UseLevel(LogLevel.Debug);
        }

        public LoggerConfiguration Information()
        {
            return _owner.UseLevel(LogLevel.Information);
        }

        public LoggerConfiguration Warning()
        {
            return _owner.UseLevel(LogLevel.Warning);
        }

        public LoggerConfiguration Error()
        {
            return _owner.UseLevel(LogLevel.Error);
        }

        public LoggerConfiguration Fatal()
        {
            return _owner.UseLevel(LogLevel.Critical);
        }
    }

    /// <summary>
    /// Serilog-style logger dispatch type configuration wrapper.
    /// </summary>
    public sealed class LogDispatchBehaviorConfiguration
    {
        private readonly LoggerConfiguration _owner;

        internal LogDispatchBehaviorConfiguration(LoggerConfiguration owner)
        {
            _owner = owner;
        }

        public LoggerConfiguration Sync()
        {
            return _owner.UseDispatchBehavior(LiteObservableLogs.LogDispatchBehavior.Sync);
        }

        public LoggerConfiguration Async()
        {
            return _owner.UseDispatchBehavior(LiteObservableLogs.LogDispatchBehavior.Async);
        }

        public LoggerConfiguration Silent()
        {
            return _owner.UseDispatchBehavior(LiteObservableLogs.LogDispatchBehavior.Silent);
        }
    }
}
