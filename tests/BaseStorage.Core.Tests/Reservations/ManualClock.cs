namespace BaseStorage.Core.Tests.Reservations;

using System;
using BaseStorage.Core.Abstractions;

/// <summary>
/// A manual IClock for the reservation tests. It never sleeps and never reads the system clock: time moves only through
/// Advance and Set. Reads counts every read of Elapsed, so the tests can check the clock-read rules (contract S3).
/// Not a test row (ADR-GWIN-023).
/// </summary>
internal sealed class ManualClock : IClock
{
    private TimeSpan current;

    /// <summary>Creates a clock that reads start until it is moved.</summary>
    /// <param name="start">The first reading.</param>
    public ManualClock(TimeSpan start)
    {
        current = start;
    }

    /// <summary>Gets the number of times Elapsed has been read. Advance and Set do not count.</summary>
    public int Reads { get; private set; }

    /// <summary>Gets the current reading and counts the read.</summary>
    public TimeSpan Elapsed
    {
        get
        {
            Reads++;
            return current;
        }
    }

    /// <summary>Moves the clock forward by an interval without reading it.</summary>
    /// <param name="by">The interval to add.</param>
    public void Advance(TimeSpan by)
    {
        current += by;
    }

    /// <summary>Sets the clock to an absolute reading without reading it.</summary>
    /// <param name="value">The new reading.</param>
    public void Set(TimeSpan value)
    {
        current = value;
    }
}
