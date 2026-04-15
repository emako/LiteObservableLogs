namespace LiteObservableLogs.Internal;

/// <summary>
/// Renders <see cref="LogEntry"/> values into a single line text format.
/// </summary>
internal sealed class ObservableLogFormatter(ObservableLoggerOptions options)
{
    private readonly ObservableLoggerOptions _options = options;

    /// <summary>
    /// Indicates whether any configured template references <c>{StackFrames}</c>.
    /// </summary>
    public bool RequiresStackFrames => ContainsStackFramesToken(_options.FileOutputTemplate)
        || ContainsStackFramesToken(_options.ConsoleOutputTemplate)
        || ContainsStackFramesToken(_options.EventOutputTemplate)
        || ContainsStackFramesToken(_options.CallbackOutputTemplate)
        || ContainsStackFramesToken(_options.OutputTemplate);

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
    /// Formats callback observer text using the configured callback template when present.
    /// </summary>
    public string FormatCallback(LogEntry entry, LogStringBuilder? sharedBuilder = null)
    {
        return FormatWithTemplateOrFallback(entry, _options.CallbackOutputTemplate, _options.OutputTemplate, sharedBuilder);
    }

    /// <summary>
    /// Uses the sink-specific template when set; otherwise the global template; otherwise <see cref="LogStringBuilder.RenderFallback"/>.
    /// </summary>
    public LogStringBuilder? CreateSharedBuilder(LogEntry entry)
    {
        bool usesAnyTemplate = HasTemplate(_options.FileOutputTemplate)
            || HasTemplate(_options.ConsoleOutputTemplate)
            || HasTemplate(_options.EventOutputTemplate)
            || HasTemplate(_options.CallbackOutputTemplate)
            || HasTemplate(_options.OutputTemplate);
        return usesAnyTemplate ? new LogStringBuilder(entry) : null;
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

    private static bool ContainsStackFramesToken(string? template)
    {
        return !string.IsNullOrWhiteSpace(template)
            && template!.IndexOf("{StackFrames", System.StringComparison.Ordinal) >= 0;
    }
}
