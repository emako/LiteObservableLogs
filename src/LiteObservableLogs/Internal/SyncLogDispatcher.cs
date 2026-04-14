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
#pragma warning disable IDE0060 // Remove unused parameter
    public void Enqueue(LogEntry entry, string fileMessage, string? consoleMessage, string? eventMessage)
#pragma warning restore IDE0060 // Remove unused parameter
    {
        // Console/Event payloads are relevant for async secondary dispatch only.
        _writer.WriteLine(entry.Timestamp, fileMessage);
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
