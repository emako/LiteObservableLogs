using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace LiteObservableLogs.Internal;

/// <summary>
/// Dispatches log entries on a dedicated background worker.
/// </summary>
internal sealed class AsyncLogDispatcher : IObservableLogDispatcher
{
    private readonly ObservableFileWriter _writer;
    private readonly BlockingCollection<(LogEntry Entry, string Message)> _queue = new(new ConcurrentQueue<(LogEntry, string)>());
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _worker;

    public AsyncLogDispatcher(ObservableFileWriter writer)
    {
        _writer = writer;
        _worker = Task.Factory.StartNew(ProcessQueue, _cts.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
    }

    public void Enqueue(LogEntry entry, string formattedMessage)
    {
        if (_queue.IsAddingCompleted)
        {
            return;
        }

        _queue.Add((entry, formattedMessage));
    }

    public void Flush()
    {
        // Wait until queue drains, then flush writer buffers for deterministic tests and shutdown.
        while (_queue.Count > 0)
        {
            Thread.Sleep(5);
        }

        _writer.Flush();
    }

    public void Dispose()
    {
        _queue.CompleteAdding();

        try
        {
            _worker.Wait();
        }
        catch (AggregateException)
        {
        }

        _cts.Cancel();
        _cts.Dispose();
        _queue.Dispose();
        _writer.Dispose();
    }

    private void ProcessQueue()
    {
        // ConsumingEnumerable blocks efficiently until data arrives or adding completes.
        foreach ((LogEntry entry, string message) in _queue.GetConsumingEnumerable(_cts.Token))
        {
            _writer.WriteLine(entry.Timestamp, message);
        }
    }
}
