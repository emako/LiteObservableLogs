using System;
using System.IO;

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
    /// Creates a detached copy so runtime components cannot mutate caller-owned options.
    /// </summary>
    public ObservableLoggerOptions Clone()
    {
        return new ObservableLoggerOptions()
        {
            LogFolder = LogFolder,
            FileName = FileName,
            DefaultCategoryName = DefaultCategoryName,
            MinLevel = MinLevel,
            LoggerType = LoggerType,
            IncludeScopes = IncludeScopes,
            IncludeCallerInfo = IncludeCallerInfo,
            IncludeCategory = IncludeCategory,
            IncludeEventId = IncludeEventId,
        };
    }
}
