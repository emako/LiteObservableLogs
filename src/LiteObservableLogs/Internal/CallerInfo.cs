namespace LiteObservableLogs.Internal;

/// <summary>
/// Caller-site metadata resolved from the stack (or supplied by the sink) for templates and fallback formatting.
/// </summary>
internal readonly struct CallerInfo(string? fileName, string? memberName, int lineNumber, int threadId)
{
    /// <summary>
    /// Source file name only (no directory). When PDB info is unavailable, this is the declaring type name.
    /// </summary>
    public string? FileName { get; } = fileName;

    /// <summary>
    /// Declaring type and method name (for example <c>MyClass.DoWork</c>), including generic arguments when applicable.
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
