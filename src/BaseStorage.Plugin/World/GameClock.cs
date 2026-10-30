namespace BaseStorage.Plugin.World;

using System;
using BaseStorage.Core.Abstractions;

/// <summary>
/// The Core clock over the network-synchronised game time (contract 4.2). Reads <c>ZNet.GetTimeSeconds</c>; a decrease, for
/// example after a reconnect, makes the base cache rebuild, which is the fail-closed rule of the clock contract. Main thread only (W2).
/// </summary>
internal sealed class GameClock : IClock
{
    /// <summary>
    /// Gets the elapsed game time. It is <see cref="TimeSpan.Zero"/> when there is no network session, or when the game value is
    /// NaN, infinite, negative or too large for a <see cref="TimeSpan"/>. It never throws.
    /// </summary>
    public TimeSpan Elapsed => ReadElapsed();

    private static TimeSpan ReadElapsed()
    {
        if (ZNet.instance == null)
        {
            return TimeSpan.Zero;
        }

        double seconds = ZNet.instance.GetTimeSeconds();
        if (double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds < 0d || seconds > TimeSpan.MaxValue.TotalSeconds)
        {
            return TimeSpan.Zero;
        }

        return TimeSpan.FromSeconds(seconds);
    }
}
