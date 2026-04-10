namespace LiteObservableLogs.Internal;

internal sealed class CallerInfo(string? fileName, string? memberName, int lineNumber, int threadId)
{
    public string? FileName { get; } = fileName;

    public string? MemberName { get; } = memberName;

    public int LineNumber { get; } = lineNumber;

    public int ThreadId { get; } = threadId;

    public string Render()
    {
        string fileName = string.IsNullOrWhiteSpace(FileName) ? "<unknown>" : FileName!;
        string memberName = string.IsNullOrWhiteSpace(MemberName) ? "<unknown>" : MemberName!;
        return LineNumber > 0 ? $"{fileName}:{LineNumber}|{memberName}" : $"{fileName}|{memberName}";
    }
}
