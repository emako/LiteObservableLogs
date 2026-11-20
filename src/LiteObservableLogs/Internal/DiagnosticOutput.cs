using System.Diagnostics;

namespace LiteObservableLogs.Internal;

/// <summary>
/// Writes diagnostic lines to <see cref="Trace"/> listeners.
/// </summary>
internal static class DiagnosticOutput
{
    private static readonly object Gate = new();

    /// <summary>
    /// Writes one line to every current trace listener.
    /// </summary>
    /// <remarks>
    /// <see cref="Debug.WriteLine(string?)"/> and <see cref="Trace.WriteLine(string?)"/> are both
    /// marked with <see cref="ConditionalAttribute"/>. A Release build of this library drops those
    /// calls, so <see cref="ConsoleTarget.Debug"/> has to touch the listeners directly.
    /// The default listener forwards the text to the attached debugger.
    /// </remarks>
    public static void WriteLine(string? message)
    {
        TraceListenerCollection listeners = Trace.Listeners;
        bool autoFlush = Trace.AutoFlush;
        if (Trace.UseGlobalLock)
        {
            lock (Gate)
            {
                WriteListeners(listeners, message, autoFlush);
            }

            return;
        }

        WriteListeners(listeners, message, autoFlush);
    }

    private static void WriteListeners(TraceListenerCollection listeners, string? message, bool autoFlush)
    {
        foreach (TraceListener listener in listeners)
        {
            if (!Trace.UseGlobalLock && !listener.IsThreadSafe)
            {
                lock (listener)
                {
                    WriteOne(listener, message, autoFlush);
                }

                continue;
            }

            WriteOne(listener, message, autoFlush);
        }
    }

    private static void WriteOne(TraceListener listener, string? message, bool autoFlush)
    {
        listener.WriteLine(message);
        if (autoFlush)
        {
            listener.Flush();
        }
    }
}
