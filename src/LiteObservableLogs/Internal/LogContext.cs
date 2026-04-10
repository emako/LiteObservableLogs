using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace LiteObservableLogs.Internal;

/// <summary>
/// Materialized view of a log entry used by output-template rendering.
/// </summary>
internal sealed class LogContext(LogEntry entry)
{
    private static readonly Regex TemplateTokenRegex = new(@"\{(?<name>[A-Za-z0-9_]+)(:(?<format>[^}]+))?\}", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly string HostUserName = Environment.UserName;

    public DateTimeOffset Timestamp { get; } = entry.Timestamp;

    public LogLevel Level { get; } = entry.Level;

    public string SourceContext { get; } = entry.Category;

    public EventId EventId { get; } = entry.EventId;

    public string Message { get; } = entry.Message;

    public Exception? Exception { get; } = entry.Exception;

    public CallerInfo? Caller { get; } = entry.Caller;

    public int? ThreadId { get; } = entry.Caller?.ThreadId;

    public IReadOnlyList<string> Scopes { get; } = entry.Scopes;

    public string Render(string template)
    {
        if (string.IsNullOrEmpty(template))
        {
            return string.Empty;
        }

        bool templateContainsExceptionToken = template.Contains("{Exception", StringComparison.Ordinal);

        return TemplateTokenRegex.Replace(template, match =>
        {
            string name = match.Groups["name"].Value;
            string? format = match.Groups["format"].Success ? match.Groups["format"].Value : null;
            return ResolveToken(name, format, templateContainsExceptionToken);
        });
    }

    private string ResolveToken(string name, string? format, bool templateContainsExceptionToken)
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
                return templateContainsExceptionToken
                    ? Message
                    : Message + (Exception?.ToString() ?? string.Empty);

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

            case "ThreadId":
                return ThreadId.HasValue
                    ? (string.IsNullOrWhiteSpace(format)
                        ? ThreadId.Value.ToString(CultureInfo.InvariantCulture)
                        : ThreadId.Value.ToString(format, CultureInfo.InvariantCulture))
                    : string.Empty;

            case "UserName":
                return HostUserName;

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

        if (string.Equals(format, "u4", StringComparison.OrdinalIgnoreCase))
        {
            return Level switch
            {
                LogLevel.Trace => "TRCE",
                LogLevel.Debug => "DBUG",
                LogLevel.Information => "INFO",
                LogLevel.Warning => "WARN",
                LogLevel.Error => "ERRO",
                LogLevel.Critical => "CRIT",
                _ => "NONE",
            };
        }

        if (string.Equals(format, "w4", StringComparison.OrdinalIgnoreCase))
        {
            return Level switch
            {
                LogLevel.Trace => "trce",
                LogLevel.Debug => "dbug",
                LogLevel.Information => "info",
                LogLevel.Warning => "warn",
                LogLevel.Error => "erro",
                LogLevel.Critical => "crit",
                _ => "none",
            };
        }

        if (string.Equals(format, "u5", StringComparison.OrdinalIgnoreCase))
        {
            return Level switch
            {
                LogLevel.Trace => "TRCEE",
                LogLevel.Debug => "DEBUG",
                LogLevel.Information => "INFO ",
                LogLevel.Warning => "WARN ",
                LogLevel.Error => "ERROR",
                LogLevel.Critical => "FATAL",
                _ => "NONE ",
            };
        }

        if (string.Equals(format, "w5", StringComparison.OrdinalIgnoreCase))
        {
            return Level switch
            {
                LogLevel.Trace => "trace",
                LogLevel.Debug => "debug",
                LogLevel.Information => "info ",
                LogLevel.Warning => "warn ",
                LogLevel.Error => "error",
                LogLevel.Critical => "fatal",
                _ => "none ",
            };
        }

        return value;
    }
}
