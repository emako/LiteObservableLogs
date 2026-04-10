using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace LiteObservableLogs.Internal;

internal static class CallerInfoResolver
{
    public static CallerInfo Resolve()
    {
        StackFrame? stackFrame = new StackTrace(true)
            .GetFrames()
            .FirstOrDefault(stackFrame =>
            {
                MethodBase? method = stackFrame.GetMethod();
                Type? declaringType = method?.DeclaringType;
                string? ns = declaringType?.Namespace;
                string? asm = declaringType?.Assembly.GetName().Name;

                if (ns != null && ns.StartsWith("LiteObservableLogs", StringComparison.Ordinal))
                {
                    return false;
                }

                if (ns != null && ns.StartsWith("Microsoft.Extensions.Logging", StringComparison.Ordinal))
                {
                    return false;
                }

                if (asm != null && asm.StartsWith("Microsoft.Extensions.Logging", StringComparison.Ordinal))
                {
                    return false;
                }

                return true;
            });

        if (stackFrame == null)
        {
            return new CallerInfo(null, null, -0, Thread.CurrentThread.ManagedThreadId);
        }

        string? fileName = stackFrame.GetFileName();
        int fileLineNumber = stackFrame.GetFileLineNumber();
        string? methodName = stackFrame.GetMethod()?.Name;

        return new CallerInfo(
            fileName: fileName is null ? "<unknown>" : Path.GetFileName(fileName),
            memberName: methodName,
            lineNumber: fileLineNumber,
            threadId: Thread.CurrentThread.ManagedThreadId);
    }
}
