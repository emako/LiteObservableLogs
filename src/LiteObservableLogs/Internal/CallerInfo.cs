namespace LiteObservableLogs.Internal;

/// <summary>
/// Caller-site metadata resolved from the stack (or supplied by the sink) for templates and fallback formatting.
/// </summary>
internal readonly struct CallerInfo(string? fileName, string? memberName, int lineNumber, int threadId)
{
    /// <summary>
    /// Source file name only (no directory), or a placeholder when PDB line info is unavailable.
    /// </summary>
    public string? FileName { get; } = fileName;

    /// <summary>
    /// Method name, including generic arity display when applicable.
    /// </summary>
    public string? MemberName { get; } = memberName;

    /// <summary>
    /// Line number from debug symbols when available; otherwise 0.
    /// </summary>
    public int LineNumber { get; } = lineNumber;

    /// <summary>
    /// Managed thread ID of the logging call site.
    /// </summary>
    public int ThreadId { get; } = threadId;
}
