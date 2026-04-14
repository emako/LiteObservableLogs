namespace LiteObservableLogs.Internal;

/// <summary>
/// Renders <see cref="LogEntry"/> values into a single line text format.
/// </summary>
internal sealed class ObservableLogFormatter(ObservableLoggerOptions options)
{
    private readonly ObservableLoggerOptions _options = options;
    private readonly bool _usesAnyTemplate = HasTemplate(options.FileOutputTemplate)
        || HasTemplate(options.ConsoleOutputTemplate)
        || HasTemplate(options.EventOutputTemplate)
        || HasTemplate(options.OutputTemplate);

    /// <summary>
    /// Formats file output using the configured file template when present.
    /// </summary>
    public string FormatFile(LogEntry entry, LogStringBuilder? sharedBuilder = null)
    {
        return FormatWithTemplateOrFallback(entry, _options.FileOutputTemplate, _options.OutputTemplate, sharedBuilder);
    }

    /// <summary>
    /// Formats console output using the configured console template when present.
    /// </summary>
    public string FormatConsole(LogEntry entry, LogStringBuilder? sharedBuilder = null)
    {
        return FormatWithTemplateOrFallback(entry, _options.ConsoleOutputTemplate, _options.OutputTemplate, sharedBuilder);
    }

    /// <summary>
    /// Formats published event text using the configured event template when present.
    /// </summary>
    public string FormatEvent(LogEntry entry, LogStringBuilder? sharedBuilder = null)
    {
        return FormatWithTemplateOrFallback(entry, _options.EventOutputTemplate, _options.OutputTemplate, sharedBuilder);
    }

    /// <summary>
    /// Uses the sink-specific template when set; otherwise the global template; otherwise <see cref="LogStringBuilder.RenderFallback"/>.
    /// </summary>
    public LogStringBuilder? CreateSharedBuilder(LogEntry entry)
    {
        return _usesAnyTemplate ? new LogStringBuilder(entry) : null;
    }

    /// <summary>
    /// Uses the sink-specific template when set; otherwise the global template; otherwise <see cref="LogStringBuilder.RenderFallback"/>.
    /// </summary>
    private string FormatWithTemplateOrFallback(LogEntry entry, string? sinkTemplate, string? globalTemplate, LogStringBuilder? sharedBuilder)
    {
        string? template = string.IsNullOrWhiteSpace(sinkTemplate) ? globalTemplate : sinkTemplate;
        if (!string.IsNullOrWhiteSpace(template))
        {
            return (sharedBuilder ?? new LogStringBuilder(entry)).Render(template!);
        }

        return LogStringBuilder.RenderFallback(entry, _options);
    }

    private static bool HasTemplate(string? template)
    {
        return !string.IsNullOrWhiteSpace(template);
    }
}
