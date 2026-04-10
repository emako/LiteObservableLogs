using System;
using Microsoft.Extensions.Logging;

namespace LiteObservableLogs.Providers;

/// <summary>
/// Extension methods for adding this logger provider to Microsoft logging pipelines.
/// </summary>
public static class LoggingBuilderExtensions
{
    /// <summary>
    /// Adds the provider and lets callers configure options inline.
    /// </summary>
    public static ILoggingBuilder AddLiteObservableLogs(
        this ILoggingBuilder builder,
        Action<ObservableLoggerOptions>? configure = null)
    {
        ObservableLoggerOptions options = new();
        configure?.Invoke(options);
        return builder.AddLiteObservableLogs(options);
    }

    /// <summary>
    /// Adds the provider using a prebuilt options instance.
    /// </summary>
    /// <remarks>
    /// Also sets the default minimum level on <see cref="Microsoft.Extensions.Logging.LoggerFilterOptions"/>
    /// to match <see cref="ObservableLoggerOptions.MinLevel"/> as a Microsoft <see cref="LogLevel"/>,
    /// so calls like <c>ILogger.LogDebug</c> are not dropped by Generic Host defaults (often <see cref="LogLevel.Information"/> or higher)
    /// before they reach this provider.
    /// </remarks>
    public static ILoggingBuilder AddLiteObservableLogs(
        this ILoggingBuilder builder,
        ObservableLoggerOptions options)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (options == null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        builder.AddProvider(new ObservableLoggerProvider(options));
        builder.SetMinimumLevel(options.MinLevel);
        return builder;
    }
}
