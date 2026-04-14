namespace LiteObservableLogs;

/// <summary>
/// File rolling interval used by Serilog-style compatibility APIs.
/// </summary>
public enum RollingInterval
{
    /// <summary>
    /// No time-based rolling; a single logical file name is used until explicitly changed.
    /// </summary>
    Infinite = 0,

    /// <summary>
    /// Roll at calendar year boundaries.
    /// </summary>
    Year = 1,

    /// <summary>
    /// Roll at calendar month boundaries.
    /// </summary>
    Month = 2,

    /// <summary>
    /// Roll at calendar day boundaries.
    /// </summary>
    Day = 3,

    /// <summary>
    /// Roll at hour boundaries.
    /// </summary>
    Hour = 4,

    /// <summary>
    /// Roll at minute boundaries.
    /// </summary>
    Minute = 5,
}
