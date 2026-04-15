using System;

namespace LiteObservableLogs.Internal;

/// <summary>
/// Zero-cost scope token returned when scope capture is disabled or unavailable.
/// </summary>
internal sealed class NoneScope : IDisposable
{
    /// <summary>
    /// Shared instance for no-op scopes.
    /// </summary>
    public static NoneScope Instance { get; } = new();

    private NoneScope()
    {
    }

    /// <inheritdoc />
    public void Dispose()
    {
    }
}
