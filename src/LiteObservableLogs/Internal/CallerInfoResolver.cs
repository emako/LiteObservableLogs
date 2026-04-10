using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
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

                if (asm != null && asm == nameof(LiteObservableLogs))
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
        string? methodName = RenderMethod(stackFrame.GetMethod());

        return new CallerInfo(
            fileName: fileName is null ? "<unknown>" : Path.GetFileName(fileName),
            memberName: methodName,
            lineNumber: fileLineNumber,
            threadId: Thread.CurrentThread.ManagedThreadId);

        static string RenderMethod(MethodBase method)
        {
            if (method is MethodInfo info && info.IsGenericMethod)
            {
                // Append method parameters
                StringBuilder result = new(method.Name);

                Type[] genericArguments = info.GetGenericArguments();
                int i = 0;
                bool flag = true;

                result.Append("<");

                while (i < genericArguments.Length)
                {
                    if (!flag)
                    {
                        result.Append(",");
                    }
                    else
                    {
                        flag = false;
                    }

                    result.Append(genericArguments[i].Name);

                    i++;
                }

                result.Append(">");

                return result.ToString();
            }
            else
            {
                return method.Name;
            }
        }
    }
}
