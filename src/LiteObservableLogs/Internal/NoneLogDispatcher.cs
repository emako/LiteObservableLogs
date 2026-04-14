namespace LiteObservableLogs.Internal;

#pragma warning disable IDE0079 // Remove unnecessary suppression
#pragma warning disable IDE0060 // Remove unused parameter

/// <summary>
/// No-op dispatcher used when <see cref="LogDispatchBehavior.Silent"/> is selected.
/// </summary>
internal sealed class NoneLogDispatcher : IObservableLogDispatcher
{
    /// <inheritdoc />
    public void Enqueue(LogEntry entry, string fileMessage, string? consoleMessage, string? eventMessage)
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

#pragma warning restore IDE0060 // Remove unused parameter
#pragma warning restore IDE0079 // Remove unnecessary suppression
