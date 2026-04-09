namespace LiteObservableLogs.Internal;

internal sealed class CallerInfo
{
    public CallerInfo(string? fileName, string? memberName, int lineNumber)
    {
        FileName = fileName;
        MemberName = memberName;
        LineNumber = lineNumber;
    }

    public string? FileName { get; }

    public string? MemberName { get; }

    public int LineNumber { get; }

    public string Render()
    {
        string fileName = string.IsNullOrWhiteSpace(FileName) ? "<unknown>" : FileName!;
        string memberName = string.IsNullOrWhiteSpace(MemberName) ? "<unknown>" : MemberName!;
        return LineNumber > 0 ? $"{fileName}:{LineNumber}|{memberName}" : $"{fileName}|{memberName}";
    }
}
