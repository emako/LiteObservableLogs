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
        return builder;
    }
}
