namespace BaseStorage.Plugin.Organize;

using System;
using BaseStorage.Core.Abstractions;
using BaseStorage.Core.Bases.Resolution;
using BaseStorage.Core.Organize.Run;
using BaseStorage.Core.Settings;
using BaseStorage.Core.Snapshots;
using BaseStorage.Plugin.BaseMenu;
using BaseStorage.Plugin.ChestCategories;
using BaseStorage.Plugin.Deposit;
using BaseStorage.Plugin.Patching;
using BaseStorage.Plugin.Reservations;
using BaseStorage.Plugin.World;
using UnityEngine;

/// <summary>
/// The plugin-side entry point of the Organize run (contract organize-runner-contracts.md section 5). It owns the one
/// coordinator, forwards the frame tick behind the fail-closed wrapper, polls the OrganizeKey and forwards the popup buttons.
/// Every decision of a run is the coordinator's. Game-bound, so it is not linked into the tests; its facts are proven by source scans.
/// </summary>
internal static class OrganizeRunner
{
    /// <summary>The patch key used by the fail-closed wrapper of the per-frame tick.</summary>
    internal const string TickKey = "BaseStorage.Plugin.Organize.OrganizeRunner.Tick";

    private static BaseStorageSettings? settings;
    private static IClock? clock;

    /// <summary>Gets the coordinator, or null until <see cref="Initialize"/> has run.</summary>
    internal static OrganizeRunCoordinator? Current { get; private set; }

    /// <summary>Creates the one coordinator at boot. Later calls return the existing instance.</summary>
    /// <param name="world">The plugin world; its cache resolves the base and its registries back the port and the lock store.</param>
    /// <param name="reservations">The reservation driver of the plugin.</param>
    /// <param name="clock">The clock.</param>
    /// <param name="settings">The settings.</param>
    /// <param name="sink">The log sink of the summary line.</param>
    /// <returns>The coordinator.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static OrganizeRunCoordinator Initialize(
        PluginWorld world,
        ReservationDriver<Container> reservations,
        IClock clock,
        BaseStorageSettings settings,
        ILogSink sink)
    {
        if (world == null)
        {
            throw new ArgumentNullException(nameof(world));
        }

        if (reservations == null)
        {
            throw new ArgumentNullException(nameof(reservations));
        }

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
            OrganizeRunner.settings = settings;
            OrganizeRunner.clock = clock;
            ChestCategoryWriter.QuietLog = sink;
            var port = new GameOrganizeRunPort(world, reservations);
            Current = new OrganizeRunCoordinator(port, world.Cache, new GameOrganizeLockStore(world), clock, settings, sink);
        }

        return Current;
    }

    /// <summary>Advances the run by one frame and polls the hotkey, behind the fail-closed wrapper.</summary>
    internal static void Tick()
    {
        PatchSafety.Run(TickKey, static () => Step());
    }

    /// <summary>Starts a run for the base containing a start (the station panel entry point of S-E004-012).</summary>
    /// <param name="start">A station or position start.</param>
    /// <returns>The outcome; NoPlayer when the plugin did not boot.</returns>
    internal static OrganizeStartOutcome StartAt(BaseStart start)
    {
        OrganizeRunCoordinator? coordinator = Current;
        return coordinator == null ? OrganizeStartOutcome.NoPlayer : coordinator.Start(start);
    }

    /// <summary>Starts a Deposit for the base containing a start (the station panel entry point of S-E004-015).</summary>
    /// <param name="start">A station or position start.</param>
    /// <returns>The outcome; NoPlayer when the plugin did not boot.</returns>
    internal static OrganizeStartOutcome StartDepositAt(BaseStart start)
    {
        OrganizeRunCoordinator? coordinator = Current;
        return coordinator == null ? OrganizeStartOutcome.NoPlayer : coordinator.StartDeposit(start);
    }

    /// <summary>Forwards the popup's yes button.</summary>
    internal static void Confirm()
    {
        Current?.Confirm();
    }

    /// <summary>Forwards the popup's no button, or the absence of a popup.</summary>
    internal static void Cancel()
    {
        Current?.Cancel();
    }

    private static void Step()
    {
        ChestHighlight.Tick();
        OrganizeRunCoordinator? coordinator = Current;
        BaseStorageSettings? current = settings;
        IClock? time = clock;
        if (coordinator == null || current == null || time == null)
        {
            return;
        }

        coordinator.Tick();
        if (coordinator.State == OrganizeRunState.Idle && OrganizeHotkey.IsPressed(current, time))
        {
            Player player = Player.m_localPlayer;
            if (player != null)
            {
                Vector3 at = player.transform.position;
                coordinator.Start(BaseStart.FromPosition(new WorldPosition(at.x, at.y, at.z)));
            }
        }
        else if (coordinator.State == OrganizeRunState.Idle && DepositHotkey.IsPressed(current, time))
        {
            Player player = Player.m_localPlayer;
            if (player != null)
            {
                Vector3 at = player.transform.position;
                coordinator.StartDeposit(BaseStart.FromPosition(new WorldPosition(at.x, at.y, at.z)));
            }
        }
    }
}
