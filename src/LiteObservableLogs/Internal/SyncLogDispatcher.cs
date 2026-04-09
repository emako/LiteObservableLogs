namespace LiteObservableLogs.Internal;

internal sealed class SyncLogDispatcher : IObservableLogDispatcher
{
    private readonly ObservableFileWriter _writer;

    public SyncLogDispatcher(ObservableFileWriter writer)
    {
        _writer = writer;
    }

    public void Enqueue(LogEntry entry, string formattedMessage)
    {
        _writer.WriteLine(entry.Timestamp, formattedMessage);
    }

    public void Flush()
    {
        _writer.Flush();
    }

    public void Dispose()
    {
        _writer.Dispose();
    }
}
