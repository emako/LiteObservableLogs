using System.Text;

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
    /// Converts a log entry into pipe-delimited output ready for file writing.
    /// </summary>
    public string Format(LogEntry entry)
    {
        StringBuilder sb = new();
        sb.Append(RenderLevel((Microsoft.Extensions.Logging.LogLevel)entry.Level))
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
    private static string RenderLevel(Microsoft.Extensions.Logging.LogLevel level)
    {
        return level switch
        {
            Microsoft.Extensions.Logging.LogLevel.Trace => "TRACE",
            Microsoft.Extensions.Logging.LogLevel.Debug => "DEBUG",
            Microsoft.Extensions.Logging.LogLevel.Information => "INFO",
            Microsoft.Extensions.Logging.LogLevel.Warning => "WARN",
            Microsoft.Extensions.Logging.LogLevel.Error => "ERROR",
            Microsoft.Extensions.Logging.LogLevel.Critical => "FATAL",
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
