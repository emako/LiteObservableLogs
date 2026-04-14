namespace LiteObservableLogs.Internal;

/// <summary>
/// No-op dispatcher used when <see cref="LogDispatchBehavior.Silent"/> is selected.
/// </summary>
internal sealed class NoneLogDispatcher : IObservableLogDispatcher
{
    /// <inheritdoc />
    public void Enqueue(LogEntry entry, string fileMessage, string? consoleMessage, string? eventMessage)
    {
        _ = entry;
        _ = fileMessage;
        _ = consoleMessage;
        _ = eventMessage;
    }

    /// <inheritdoc />
    public void Flush()
    {
    }

    /// <inheritdoc />
    public void Dispose()
    {
    }
}
