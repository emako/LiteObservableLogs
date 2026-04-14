namespace LiteObservableLogs.Internal;

/// <summary>
/// Renders <see cref="LogEntry"/> values into a single line text format.
/// </summary>
internal sealed class ObservableLogFormatter
{
    private readonly ObservableLoggerOptions _options;

    public ObservableLogFormatter(ObservableLoggerOptions options)
    {
        _options = options;
    }

    /// <summary>
    /// Formats file output using the configured file template when present.
    /// </summary>
    public string FormatFile(LogEntry entry)
    {
        return FormatWithTemplateOrFallback(entry, _options.FileOutputTemplate, _options.OutputTemplate);
    }

    /// <summary>
    /// Formats console output using the configured console template when present.
    /// </summary>
    public string FormatConsole(LogEntry entry)
    {
        return FormatWithTemplateOrFallback(entry, _options.ConsoleOutputTemplate, _options.OutputTemplate);
    }

    /// <summary>
    /// Formats published event text using the configured event template when present.
    /// </summary>
    public string FormatEvent(LogEntry entry)
    {
        return FormatWithTemplateOrFallback(entry, _options.EventOutputTemplate, _options.OutputTemplate);
    }

    private string FormatWithTemplateOrFallback(LogEntry entry, string? sinkTemplate, string? globalTemplate)
    {
        string? template = string.IsNullOrWhiteSpace(sinkTemplate) ? globalTemplate : sinkTemplate;
        if (!string.IsNullOrWhiteSpace(template))
        {
            return new LogStringBuilder(entry).Render(template!);
        }

        return LogStringBuilder.RenderFallback(entry, _options);
    }
}
