using System;

namespace LiteObservableLogs.Internal;

/// <summary>
/// Zero-cost scope token returned when scope capture is disabled or unavailable.
/// </summary>
internal sealed class NullScope : IDisposable
{
    /// <summary>Shared instance for no-op scopes.</summary>
    public static NullScope Instance { get; } = new();

    private NullScope()
    {
    }

    /// <inheritdoc />
    public void Dispose()
    {
    }
}
