using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace LiteObservableLogs.Internal;

/// <summary>
/// Dispatches log entries on a dedicated background worker.
/// Bounded by producer activity and consumed by a single background worker.
/// The worker blocks on <see cref="BlockingCollection{T}.GetConsumingEnumerable()"/>
/// when no items are available, and wakes only when producers call <see cref="Enqueue"/>.
/// This provides wait-for-work semantics (no busy spinning) while preserving FIFO queue behavior.
/// </summary>
internal sealed class AsyncLogDispatcher : IObservableLogDispatcher
{
    private readonly ObservableFileWriter _writer;
    private readonly Func<LogEntry, (string FileMessage, string? ConsoleMessage, string? EventMessage)> _render;
    private readonly Action<LogEntry, string?, string?>? _afterWrite;
    private readonly BlockingCollection<LogEntry> _queue = new(new ConcurrentQueue<LogEntry>());
    private readonly Task _worker;
    private readonly ManualResetEventSlim _drainedSignal = new(initialState: true);
    private int _pendingCount;

    /// <inheritdoc />
    public string? CurrentLogFilePath => _writer.CurrentLogFilePath;

    /// <summary>
    /// Starts a long-running worker that drains the queue and writes to <paramref name="writer"/>.
    /// </summary>
    /// <param name="writer">Shared file writer for serialized output.</param>
    /// <param name="render">Formats file/console/event payloads on the worker thread.</param>
    /// <param name="afterWrite">Optional hook invoked after each successful line write (for example console/event sinks).</param>
    public AsyncLogDispatcher(
        ObservableFileWriter writer,
        Func<LogEntry, (string FileMessage, string? ConsoleMessage, string? EventMessage)> render,
        Action<LogEntry, string?, string?>? afterWrite = null)
    {
        _writer = writer;
        _render = render;
        _afterWrite = afterWrite;
        _worker = Task.Factory.StartNew(ProcessQueue, TaskCreationOptions.LongRunning);
    }

    /// <inheritdoc />
    public void Enqueue(LogEntry entry, string fileMessage, string? consoleMessage, string? eventMessage)
    {
        if (_queue.IsAddingCompleted)
        {
            return;
        }

        Interlocked.Increment(ref _pendingCount);
        _drainedSignal.Reset();
        try
        {
            _queue.Add(entry);
        }
        catch (InvalidOperationException)
        {
            MarkOneItemProcessed();
        }
    }

    /// <inheritdoc />
    public void Flush()
    {
        _drainedSignal.Wait();
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
        catch (AggregateException ex)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[LiteObservableLogs] Async worker wait failed during dispose: {ex}");
#endif
        }

        _drainedSignal.Dispose();
        _queue.Dispose();
        _writer.Dispose();
    }

    /// <summary>
    /// Background loop: dequeue, write, flush, then invoke secondary targets.
    /// </summary>
    private void ProcessQueue()
    {
        // ConsumingEnumerable blocks efficiently until data arrives or adding completes.
        foreach (LogEntry entry in _queue.GetConsumingEnumerable())
        {
            try
            {
                (string fileMessage, string? consoleMessage, string? eventMessage) = _render(entry);
                _writer.WriteLine(entry.Timestamp, fileMessage);
                // Keep async behavior (off caller thread) but make file visibility immediate.
                _writer.Flush();
                InvokeAfterWriteSafe(entry, consoleMessage, eventMessage);
            }
            finally
            {
                MarkOneItemProcessed();
            }
        }
    }

    private void InvokeAfterWriteSafe(LogEntry entry, string? consoleMessage, string? eventMessage)
    {
        if (_afterWrite == null)
        {
            return;
        }

        try
        {
            _afterWrite(entry, consoleMessage, eventMessage);
        }
        catch (Exception ex)
        {
            // Keep background pipeline alive even when secondary outputs fail.
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[LiteObservableLogs] Secondary log dispatch failed: {ex}");
#endif
        }
    }

    private void MarkOneItemProcessed()
    {
        if (Interlocked.Decrement(ref _pendingCount) == 0)
        {
            _drainedSignal.Set();
        }
    }
}
