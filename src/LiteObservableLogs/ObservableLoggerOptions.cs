using System;
using System.IO;
using Microsoft.Extensions.Logging;

namespace LiteObservableLogs;

/// <summary>
/// Represents immutable-like configuration values used to build logger instances.
/// </summary>
public sealed class ObservableLoggerOptions
{
    /// <summary>
    /// Gets or sets the directory where log files are written.
    /// </summary>
    public string LogFolder { get; set; } = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");

    /// <summary>
    /// Gets or sets the fixed file name. When null, date-based rolling naming is used.
    /// </summary>
    public string? FileName { get; set; }

    /// <summary>
    /// Gets or sets an optional file-name template (for example: "app_{Timestamp:yyyyMMdd}.log").
    /// When set, it takes precedence over <see cref="FileName"/>.
    /// </summary>
    public string? FileNameTemplate { get; set; }

    /// <summary>
    /// Gets or sets file rolling cadence for file-name templates.
    /// </summary>
    public RollingInterval RollingInterval { get; set; } = RollingInterval.Infinite;

    /// <summary>
    /// Gets or sets the fallback category name used when no category is provided.
    /// </summary>
    public string DefaultCategoryName { get; set; } = nameof(LiteObservableLogs);

    /// <summary>
    /// Gets or sets the minimum level that can be emitted.
    /// </summary>
    public LogLevel MinLevel { get; set; } = LogLevel.Trace;

    /// <summary>
    /// Gets or sets how entries are dispatched to disk.
    /// </summary>
    public LoggerType LoggerType { get; set; } = LoggerType.Async;

    /// <summary>
    /// Gets or sets whether active logging scopes should be rendered.
    /// </summary>
    public bool IncludeScopes { get; set; } = true;

    /// <summary>
    /// Gets or sets whether caller file/member metadata should be rendered.
    /// </summary>
    public bool IncludeCallerInfo { get; set; } = true;

    /// <summary>
    /// Gets or sets whether category name should be rendered.
    /// </summary>
    public bool IncludeCategory { get; set; } = true;

    /// <summary>
    /// Gets or sets whether non-zero event identifiers should be rendered.
    /// </summary>
    public bool IncludeEventId { get; set; } = true;

    /// <summary>
    /// Gets or sets whether formatted log lines should also be mirrored to console output.
    /// </summary>
    public bool WriteToConsole { get; set; }

    /// <summary>
    /// Gets or sets which output stream is used when <see cref="WriteToConsole"/> is enabled.
    /// </summary>
    public ConsoleTarget ConsoleTarget { get; set; } = default;

    /// <summary>
    /// Gets or sets the template used for file output.
    /// </summary>
    public string? FileOutputTemplate { get; set; }

    /// <summary>
    /// Gets or sets the template used for console output.
    /// </summary>
    public string? ConsoleOutputTemplate { get; set; }

    /// <summary>
    /// Gets or sets whether log entries should be published to <see cref="Log.Received"/>.
    /// </summary>
    public bool PublishToEvent { get; set; }

    /// <summary>
    /// Gets or sets the template used for published event text.
    /// </summary>
    public string? EventOutputTemplate { get; set; }

    /// <summary>
    /// Gets or sets the global template used when a sink-specific template is not provided.
    /// </summary>
    public string? OutputTemplate { get; set; }

    /// <summary>
    /// Gets or sets the maximum retained file count. Null keeps all files.
    /// </summary>
    public int? RetainedFileCountLimit { get; set; }

    /// <summary>
    /// Gets or sets the maximum retained file age. Null keeps files regardless of age.
    /// </summary>
    public TimeSpan? RetainedFileTimeLimit { get; set; }

    /// <summary>
    /// Gets or sets the timestamp factory used when creating log entries.
    /// </summary>
    public Func<DateTimeOffset> TimestampProvider { get; set; } = static () => DateTimeOffset.Now;

    /// <summary>
    /// Creates a detached copy so runtime components cannot mutate caller-owned options.
    /// </summary>
    public ObservableLoggerOptions Clone()
    {
        return new ObservableLoggerOptions
        {
            LogFolder = LogFolder,
            FileName = FileName,
            FileNameTemplate = FileNameTemplate,
            RollingInterval = RollingInterval,
            DefaultCategoryName = DefaultCategoryName,
            MinLevel = MinLevel,
            LoggerType = LoggerType,
            IncludeScopes = IncludeScopes,
            IncludeCallerInfo = IncludeCallerInfo,
            IncludeCategory = IncludeCategory,
            IncludeEventId = IncludeEventId,
            WriteToConsole = WriteToConsole,
            ConsoleTarget = ConsoleTarget,
            FileOutputTemplate = FileOutputTemplate,
            ConsoleOutputTemplate = ConsoleOutputTemplate,
            PublishToEvent = PublishToEvent,
            EventOutputTemplate = EventOutputTemplate,
            OutputTemplate = OutputTemplate,
            RetainedFileCountLimit = RetainedFileCountLimit,
            RetainedFileTimeLimit = RetainedFileTimeLimit,
            TimestampProvider = TimestampProvider,
        };
    }
}
