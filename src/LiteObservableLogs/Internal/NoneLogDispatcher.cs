namespace LiteObservableLogs.Internal;

/// <summary>
/// No-op dispatcher used when <see cref="LogDispatchBehavior.Silent"/> is selected.
/// </summary>
internal sealed class NoneLogDispatcher : IObservableLogDispatcher
{
    /// <inheritdoc />
#pragma warning disable IDE0060 // Remove unused parameter
    public void Enqueue(LogEntry entry, string fileMessage, string? consoleMessage, string? eventMessage)
#pragma warning restore IDE0060 // Remove unused parameter
    {
        // Intentionally no-op for silent mode.
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
