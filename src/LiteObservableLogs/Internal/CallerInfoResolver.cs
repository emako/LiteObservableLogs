using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace LiteObservableLogs.Internal;

internal static class CallerInfoResolver
{
    public static CallerInfo Resolve()
    {
        StackFrame[]? frames = new StackTrace(true).GetFrames();
        if (frames == null || frames.Length == 0)
        {
            return new CallerInfo(null, null, 0);
        }

        foreach (StackFrame frame in frames)
        {
            MethodBase? method = frame.GetMethod();
            Type? declaringType = method?.DeclaringType;
            string? ns = declaringType?.Namespace;
            string? asm = declaringType?.Assembly.GetName().Name;

            if (ns != null && ns.StartsWith("LiteObservableLogs", System.StringComparison.Ordinal))
            {
                continue;
            }

            if (ns != null && ns.StartsWith("Microsoft.Extensions.Logging", System.StringComparison.Ordinal))
            {
                continue;
            }

            if (asm != null && asm.StartsWith("Microsoft.Extensions.Logging", System.StringComparison.Ordinal))
            {
                continue;
            }

            string? fileName = frame.GetFileName();
            return new CallerInfo(
                fileName == null ? declaringType?.Name : Path.GetFileName(fileName),
                method?.Name,
                frame.GetFileLineNumber());
        }

        return new CallerInfo(null, null, 0);
    }
}
