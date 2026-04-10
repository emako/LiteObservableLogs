using System;
using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace LiteObservableLogs.Internal;

/// <summary>
/// Materialized view of a log entry used by output-template rendering.
/// </summary>
internal sealed class LogContext
{
    private static readonly Regex TemplateTokenRegex = new(@"\{(?<name>[A-Za-z0-9_]+)(:(?<format>[^}]+))?\}", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public LogContext(LogEntry entry)
    {
        Timestamp = entry.Timestamp;
        Level = entry.Level;
        SourceContext = entry.Category;
        EventId = entry.EventId;
        Message = entry.Message;
        Exception = entry.Exception;
        Caller = entry.Caller;
        Scopes = entry.Scopes;
    }

    public DateTimeOffset Timestamp { get; }

    public LogLevel Level { get; }

    public string SourceContext { get; }

    public EventId EventId { get; }

    public string Message { get; }

    public Exception? Exception { get; }

    public CallerInfo? Caller { get; }

    public System.Collections.Generic.IReadOnlyList<string> Scopes { get; }

    public string Render(string template)
    {
        if (string.IsNullOrEmpty(template))
        {
            return string.Empty;
        }

        return TemplateTokenRegex.Replace(template, match =>
        {
            string name = match.Groups["name"].Value;
            string? format = match.Groups["format"].Success ? match.Groups["format"].Value : null;
            return ResolveToken(name, format);
        });
    }

    private string ResolveToken(string name, string? format)
    {
        switch (name)
        {
            case "Timestamp":
                return string.IsNullOrWhiteSpace(format)
                    ? Timestamp.ToString("O", CultureInfo.InvariantCulture)
                    : Timestamp.ToString(format, CultureInfo.InvariantCulture);
            case "Level":
                return RenderLevel(format);
            case "Message":
                return Message;
            case "Exception":
                return Exception?.ToString() ?? string.Empty;
            case "NewLine":
                return Environment.NewLine;
            case "SourceContext":
                return SourceContext;
            case "EventId":
                return EventId.Id == 0 && string.IsNullOrWhiteSpace(EventId.Name)
                    ? string.Empty
                    : string.IsNullOrWhiteSpace(EventId.Name)
                        ? EventId.Id.ToString(CultureInfo.InvariantCulture)
                        : $"{EventId.Id}:{EventId.Name}";
            case "Scopes":
                return string.Join(" => ", Scopes);
            case "Caller":
                return Caller?.Render() ?? string.Empty;
            default:
                return string.Empty;
        }
    }

    private string RenderLevel(string? format)
    {
        string value = Level switch
        {
            LogLevel.Trace => "Trace",
            LogLevel.Debug => "Debug",
            LogLevel.Information => "Information",
            LogLevel.Warning => "Warning",
            LogLevel.Error => "Error",
            LogLevel.Critical => "Fatal",
            _ => "None",
        };

        if (string.IsNullOrWhiteSpace(format))
        {
            return value;
        }

        if (string.Equals(format, "u3", StringComparison.OrdinalIgnoreCase))
        {
            return Level switch
            {
                LogLevel.Trace => "VRB",
                LogLevel.Debug => "DBG",
                LogLevel.Information => "INF",
                LogLevel.Warning => "WRN",
                LogLevel.Error => "ERR",
                LogLevel.Critical => "FTL",
                _ => "NON",
            };
        }

        if (string.Equals(format, "w3", StringComparison.OrdinalIgnoreCase))
        {
            return Level switch
            {
                LogLevel.Trace => "vrb",
                LogLevel.Debug => "dbg",
                LogLevel.Information => "inf",
                LogLevel.Warning => "wrn",
                LogLevel.Error => "err",
                LogLevel.Critical => "ftl",
                _ => "non",
            };
        }

        return value;
    }
}
