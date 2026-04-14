using System;
using System.Diagnostics;
using System.Reflection;
using System.Text;

namespace LiteObservableLogs.Internal;

/// <summary>
/// Captures caller-thread stack frames outside logging infrastructure.
/// </summary>
internal static class StackFramesResolver
{
    /// <summary>
    /// Returns lines in exception-like stack format (for example: <c>at Type.Method() in File:line N</c>).
    /// </summary>
    public static string Resolve()
    {
        StackFrame[]? frames = new StackTrace(true).GetFrames();
        if (frames == null || frames.Length == 0)
        {
            return string.Empty;
        }

        StringBuilder sb = new();
        for (int i = 0; i < frames.Length; i++)
        {
            StackFrame frame = frames[i];
            MethodBase? method = frame.GetMethod();
            Type? declaringType = method?.DeclaringType;
            string? asm = declaringType?.Assembly.GetName().Name;

            if (asm != null && asm == nameof(LiteObservableLogs))
            {
                continue;
            }

            if (asm != null && asm.StartsWith("Microsoft.Extensions.Logging", StringComparison.Ordinal))
            {
                continue;
            }

            if (method == null)
            {
                continue;
            }

            sb.Append("   at ");
            if (declaringType != null)
            {
                sb.Append(declaringType.FullName).Append('.');
            }

            sb.Append(method.Name).Append("()");

            string? fileName = frame.GetFileName();
            int lineNumber = frame.GetFileLineNumber();
            if (!string.IsNullOrWhiteSpace(fileName) && lineNumber > 0)
            {
                sb.Append(" in ").Append(fileName).Append(":line ").Append(lineNumber);
            }

            sb.AppendLine();
        }

        return sb.ToString().TrimEnd();
    }
}
