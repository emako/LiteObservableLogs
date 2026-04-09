namespace LiteObservableLogs.Internal;

internal sealed class NoneLogDispatcher : IObservableLogDispatcher
{
    public void Enqueue(LogEntry entry, string formattedMessage)
    {
    }

    public void Flush()
    {
    }

    public void Dispose()
    {
    }
}
