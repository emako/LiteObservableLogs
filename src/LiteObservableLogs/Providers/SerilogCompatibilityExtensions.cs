using Microsoft.Extensions.Logging;
using System;

namespace LiteObservableLogs.Providers;

/// <summary>
/// Compatibility extensions so callers can keep using AddSerilog style code.
/// </summary>
public static class SerilogCompatibilityExtensions
{
    /// <summary>
    /// Adds LiteObservableLogs provider using current global logger options.
    /// </summary>
    public static ILoggingBuilder AddSerilog(this ILoggingBuilder builder)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        return builder.AddLiteObservableLogs(Log.CurrentOptions);
    }
}
