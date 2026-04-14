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
    private readonly Action<LogEntry>? _afterWrite;
    private readonly BlockingCollection<(LogEntry Entry, string Message)> _queue = new(new ConcurrentQueue<(LogEntry, string)>());
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _worker;
    private int _pendingCount;

    /// <summary>
    /// Starts a long-running worker that drains the queue and writes to <paramref name="writer"/>.
    /// </summary>
    /// <param name="writer">Shared file writer for serialized output.</param>
    /// <param name="afterWrite">Optional hook invoked after each successful line write (for example console/event sinks).</param>
    public AsyncLogDispatcher(ObservableFileWriter writer, Action<LogEntry>? afterWrite = null)
    {
        _writer = writer;
        _afterWrite = afterWrite;
        _worker = Task.Factory.StartNew(ProcessQueue, _cts.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
    }

    /// <inheritdoc />
    public void Enqueue(LogEntry entry, string formattedMessage)
    {
        if (_queue.IsAddingCompleted)
        {
            return;
        }

        Interlocked.Increment(ref _pendingCount);
        _queue.Add((entry, formattedMessage));
    }

    /// <inheritdoc />
    public void Flush()
    {
        // Wait until queue drains, then flush writer buffers for deterministic tests and shutdown.
        while (Volatile.Read(ref _pendingCount) > 0)
        {
            Thread.Sleep(5);
        }

        _writer.Flush();
    }

    /// <inheritdoc />
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

    /// <summary>Background loop: dequeue, write, flush, then invoke secondary targets.</summary>
    private void ProcessQueue()
    {
        // ConsumingEnumerable blocks efficiently until data arrives or adding completes.
        foreach ((LogEntry entry, string message) in _queue.GetConsumingEnumerable(_cts.Token))
        {
            try
            {
                _writer.WriteLine(entry.Timestamp, message);
                // Keep async behavior (off caller thread) but make file visibility immediate.
                _writer.Flush();
                _afterWrite?.Invoke(entry);
            }
            finally
            {
                Interlocked.Decrement(ref _pendingCount);
            }
        }
    }
}
