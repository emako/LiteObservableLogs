namespace LiteObservableLogs;

/// <summary>
/// File rolling interval used by Serilog-style compatibility APIs.
/// </summary>
public enum RollingInterval
{
    Infinite = 0,
    Year = 1,
    Month = 2,
    Day = 3,
    Hour = 4,
    Minute = 5,
}
