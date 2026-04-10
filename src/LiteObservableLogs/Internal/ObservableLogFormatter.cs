using System.Text;
using Microsoft.Extensions.Logging;

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
            return new LogContext(entry).Render(template!);
        }

        return FormatFallback(entry);
    }

    /// <summary>
    /// Converts a log entry into the legacy pipe-delimited default output.
    /// </summary>
    private string FormatFallback(LogEntry entry)
    {
        StringBuilder sb = new();
        sb.Append(RenderLevel(entry.Level))
          .Append('|')
          .Append(entry.Timestamp.ToString("yyyy-MM-dd|HH:mm:ss.fff"));

        if (_options.IncludeCategory)
        {
            sb.Append('|').Append(Sanitize(entry.Category));
        }

        if (_options.IncludeEventId && entry.EventId.Id != 0)
        {
            sb.Append('|').Append("EventId=").Append(entry.EventId.Id);
            if (!string.IsNullOrWhiteSpace(entry.EventId.Name))
            {
                sb.Append('(').Append(Sanitize(entry.EventId.Name!)).Append(')');
            }
        }

        if (_options.IncludeScopes && entry.Scopes.Count > 0)
        {
            sb.Append('|').Append("Scopes=").Append(Sanitize(string.Join(" => ", entry.Scopes)));
        }

        if (_options.IncludeCallerInfo && entry.Caller is CallerInfo caller)
        {
            sb.Append('|').Append(Sanitize(caller.Render()));
        }

        sb.Append('|').Append(Sanitize(entry.Message));

        if (entry.Exception != null)
        {
            sb.Append('|').Append("Exception=").Append(Sanitize(entry.Exception.ToString()));
        }

        return sb.ToString();
    }

    /// <summary>
    /// Maps Microsoft log levels to compact textual tokens.
    /// </summary>
    private static string RenderLevel(LogLevel level)
    {
        return level switch
        {
            LogLevel.Trace => "TRACE",
            LogLevel.Debug => "DEBUG",
            LogLevel.Information => "INFO",
            LogLevel.Warning => "WARN",
            LogLevel.Error => "ERROR",
            LogLevel.Critical => "FATAL",
            _ => "NONE",
        };
    }

    /// <summary>
    /// Escapes line breaks so every entry stays single-line in the target file.
    /// </summary>
    private static string Sanitize(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        return value
            .Replace("\r\n", "\\n")
            .Replace("\n", "\\n")
            .Replace("\r", string.Empty);
    }
}
