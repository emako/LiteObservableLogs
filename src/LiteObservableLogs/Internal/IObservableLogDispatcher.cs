using System;

namespace LiteObservableLogs.Internal;

internal interface IObservableLogDispatcher : IDisposable
{
    public void Enqueue(LogEntry entry, string formattedMessage);

    public void Flush();
}
