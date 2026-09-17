using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;

namespace LiteObservableLogs.Internal;

/// <summary>
/// Walks the stack to find the first frame outside this library and Microsoft.Extensions.Logging,
/// producing <see cref="CallerInfo"/> for optional inclusion in log output.
/// </summary>
/// <remarks>
/// Does not use a fixed skip-frame count: packers (for example Virbox JIT) may insert empty frames
/// between the logger and the business call site. When PDB line info is missing, the declaring
/// type name is used in place of the file name.
/// </remarks>
internal static class CallerInfoResolver
{
    /// <summary>
    /// Resolves caller metadata for the current thread, or placeholders when no suitable frame exists.
    /// </summary>
    public static CallerInfo Resolve()
    {
        StackFrame[]? frames = new StackTrace(1, true).GetFrames();
        if (frames == null || frames.Length == 0)
        {
            return new CallerInfo("<unknown>", "<unknown>", 0, Thread.CurrentThread.ManagedThreadId);
        }

        foreach (StackFrame frame in frames)
        {
            MethodBase? method = frame.GetMethod();
            if (LoggingStackFrameFilter.IsLoggingInfrastructure(method))
            {
                continue;
            }

            Type declaringType = method!.DeclaringType!;
            string fileName = Path.GetFileName(frame.GetFileName()) ?? string.Empty;
            if (string.IsNullOrEmpty(fileName))
            {
                // Release builds without PDB: fall back to the declaring type name.
                fileName = declaringType.Name;
            }

            int lineNumber = frame.GetFileLineNumber();
            string memberName = RenderMemberName(declaringType, method);

            return new CallerInfo(
                fileName: fileName,
                memberName: memberName,
                lineNumber: lineNumber,
                threadId: Thread.CurrentThread.ManagedThreadId);
        }

        return new CallerInfo("<unknown>", "<unknown>", 0, Thread.CurrentThread.ManagedThreadId);
    }

    private static string RenderMemberName(Type declaringType, MethodBase method)
    {
        StringBuilder result = new();
        result.Append(declaringType.Name);
        result.Append('.');
        result.Append(method.Name);
        AppendGenericArguments(result, method);
        return result.ToString();
    }

    private static void AppendGenericArguments(StringBuilder result, MethodBase method)
    {
        if (method is not MethodInfo methodInfo || !methodInfo.IsGenericMethod)
        {
            return;
        }

        Type[] genericArguments = methodInfo.GetGenericArguments();
        result.Append('<');
        for (int i = 0; i < genericArguments.Length; i++)
        {
            if (i > 0)
            {
                result.Append(',');
            }

            result.Append(genericArguments[i].Name);
        }

        result.Append('>');
    }
}
