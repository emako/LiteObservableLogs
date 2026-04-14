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
    /// Writes to debug listeners via <see cref="System.Diagnostics.Debug.WriteLine(string?)"/>.
    /// </summary>
    Debug,
}
