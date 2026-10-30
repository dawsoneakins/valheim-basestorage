namespace BaseStorage.Core.Tests.Locking;

using System;
using BaseStorage.Core.Abstractions;

/// <summary>
/// A clock that the test moves by hand and that counts its reads (ADR-GWIN-027). It starts at 3600 s, as in contract 3.1. Every read of
/// <see cref="Elapsed"/> increments <see cref="Reads"/> before <see cref="Throws"/> is thrown, so a test must not read the property directly;
/// assert on <see cref="Reads"/> only.
/// </summary>
internal sealed class CountingClock : IClock
{
    private TimeSpan now = TimeSpan.FromSeconds(3600);

    /// <summary>Gets the number of reads of <see cref="Elapsed"/> so far.</summary>
    public int Reads { get; private set; }

    /// <summary>When set, every read throws this exception after it is counted.</summary>
    public Exception? Throws { get; set; }

    /// <summary>Gets or sets the reading. A test sets it lower to model a clock that goes backwards.</summary>
    public TimeSpan Elapsed
    {
        get
        {
            Reads++;
            if (Throws is not null)
            {
                throw Throws;
            }

            return now;
        }

        set => now = value;
    }
}
