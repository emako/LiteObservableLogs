using System;
using System.Reflection;

namespace LiteObservableLogs.Internal;

/// <summary>
/// Identifies stack frames that belong to this library or Microsoft.Extensions.Logging,
/// including empty frames inserted by some packers (for example Virbox JIT).
/// </summary>
internal static class LoggingStackFrameFilter
{
    /// <summary>
    /// Returns <c>true</c> when the frame should be skipped while searching for the business call site.
    /// </summary>
    public static bool IsLoggingInfrastructure(MethodBase? method)
    {
        // Packers may insert frames with no resolvable method between logger and business code.
        if (method == null)
        {
            return true;
        }

        Type? declaringType = method.DeclaringType;
        if (declaringType == null || string.IsNullOrEmpty(method.Name))
        {
            return true;
        }

        string? assemblyName = declaringType.Assembly.GetName().Name;
        if (assemblyName != null)
        {
            if (assemblyName == nameof(LiteObservableLogs))
            {
                return true;
            }

            if (assemblyName.StartsWith("Microsoft.Extensions.Logging", StringComparison.Ordinal))
            {
                return true;
            }
        }

        string? ns = declaringType.Namespace;
        if (string.IsNullOrEmpty(ns))
        {
            return false;
        }

        if (ns.StartsWith("Microsoft.Extensions.Logging", StringComparison.Ordinal))
        {
            return true;
        }

        // Match library namespaces only — do not treat LiteObservableLogs.Demo.* as infrastructure.
        return ns == nameof(LiteObservableLogs)
            || ns.StartsWith("LiteObservableLogs.Internal", StringComparison.Ordinal)
            || ns.StartsWith("LiteObservableLogs.Providers", StringComparison.Ordinal);
    }
}
