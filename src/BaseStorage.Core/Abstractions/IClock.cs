namespace BaseStorage.Core.Abstractions;

using System;

/// <summary>A source of elapsed time with an arbitrary epoch; only differences between readings are meaningful.</summary>
public interface IClock
{
    /// <summary>
    /// Gets the time elapsed since the clock's epoch. The Plugin uses the game's network-synchronised time.
    /// Readings are expected to be non-decreasing but that is not guaranteed; consumers treat a decrease as
    /// "every interval has elapsed". Never throws.
    /// </summary>
    TimeSpan Elapsed { get; }
}
