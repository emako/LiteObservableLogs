namespace LiteObservableLogs;

/// <summary>
/// Selects the output channel when console mirroring is enabled.
/// </summary>
public enum ConsoleTarget
{
    /// <summary>
    /// Writes to standard output via <see cref="System.Console.WriteLine(string?)"/>.
    /// </summary>
    Console,

    /// <summary>
    /// Writes to <see cref="System.Diagnostics.Trace"/> listeners.
    /// The default listener forwards the line to the attached debugger, so it appears in the Debug output window.
    /// </summary>
    Debug,
}
