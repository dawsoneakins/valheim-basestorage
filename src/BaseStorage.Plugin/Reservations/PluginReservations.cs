namespace BaseStorage.Plugin.Reservations;

using System;
using BaseStorage.Core.Abstractions;
using BaseStorage.Core.Settings;
using BaseStorage.Core.Ui;
using BaseStorage.Plugin.Patching;

/// <summary>
/// The plugin-side entry point of the chest reservation adapter (contract S-E005-002 section 4.2). It owns the one driver,
/// forwards the frame tick and the two patch bodies, and returns "run vanilla" whenever no driver exists. Game-bound, so it is
/// not linked into the tests; its facts are proven by source scans.
/// </summary>
internal static class PluginReservations
{
    /// <summary>The patch key used by the fail-closed wrapper of the per-frame tick.</summary>
    internal const string TickKey = "BaseStorage.Plugin.Reservations.PluginReservations.Tick";

    /// <summary>Gets the driver, or null until <see cref="Initialize"/> has run.</summary>
    internal static ReservationDriver<Container>? Current { get; private set; }

    /// <summary>Creates the one driver at boot. Later calls return the existing instance.</summary>
    /// <param name="clock">The clock.</param>
    /// <param name="settings">The settings; the reservation timeout is read once here.</param>
    /// <param name="sink">The log sink.</param>
    /// <returns>The driver.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static ReservationDriver<Container> Initialize(IClock clock, BaseStorageSettings settings, ILogSink sink)
    {
        if (clock == null)
        {
            throw new ArgumentNullException(nameof(clock));
        }

        if (settings == null)
        {
            throw new ArgumentNullException(nameof(settings));
        }

        if (sink == null)
        {
            throw new ArgumentNullException(nameof(sink));
        }

        if (Current == null)
        {
            Current = new ReservationDriver<Container>(new GameReservationPort(), clock, settings.ReserveTimeoutMs, StringTable.English, sink);
        }

        return Current;
    }

    /// <summary>Advances the reservations by one frame, behind the fail-closed wrapper.</summary>
    internal static void Tick()
    {
        PatchSafety.Run(TickKey, static () => Current?.Tick());
    }

    /// <summary>Forwards an open response to the driver.</summary>
    /// <param name="chest">The chest, which may be a destroyed Unity object.</param>
    /// <param name="granted">True when the owner granted the request.</param>
    /// <returns>False when the response was consumed; true when vanilla runs.</returns>
    internal static bool OnOpenResponse(Container chest, bool granted)
    {
        ReservationDriver<Container>? driver = Current;
        if (driver == null || chest == null)
        {
            return true;
        }

        return driver.OnOpenResponse(chest, granted);
    }

    /// <summary>Forwards a local interaction to the driver and sets the result when the probe handled it.</summary>
    /// <param name="chest">The chest, which may be a destroyed Unity object.</param>
    /// <param name="hold">True when the interaction is a hold.</param>
    /// <param name="alt">True when the alternative interaction is used.</param>
    /// <param name="result">The result of the interaction, set to true when the probe handled it.</param>
    /// <returns>False when the probe handled the interaction; true when vanilla runs.</returns>
    internal static bool OnInteract(Container chest, bool hold, bool alt, ref bool result)
    {
        ReservationDriver<Container>? driver = Current;
        if (driver == null || chest == null)
        {
            return true;
        }

        bool run = driver.OnLocalInteract(chest, hold, alt, BaseStoragePlugin.Settings?.DebugLogging == true);
        if (!run)
        {
            result = true;
        }

        return run;
    }
}
