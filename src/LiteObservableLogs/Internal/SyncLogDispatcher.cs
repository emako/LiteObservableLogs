namespace LiteObservableLogs.Internal;

/// <summary>
/// Writes each formatted line on the caller thread and flushes immediately for strict durability ordering.
/// </summary>
/// <remarks>
/// Creates a dispatcher that writes through the shared <see cref="ObservableFileWriter"/>.
/// </remarks>
internal sealed class SyncLogDispatcher(ObservableFileWriter writer) : IObservableLogDispatcher
{
    private readonly ObservableFileWriter _writer = writer;

    /// <inheritdoc />
    public void Enqueue(LogEntry entry, string formattedMessage)
    {
        _writer.WriteLine(entry.Timestamp, formattedMessage);
        // Sync mode guarantees visibility as soon as the call returns.
        _writer.Flush();
    }

    /// <inheritdoc />
    public void Flush()
    {
        _writer.Flush();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _writer.Dispose();
    }
}
