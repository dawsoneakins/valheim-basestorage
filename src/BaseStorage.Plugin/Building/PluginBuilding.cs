namespace BaseStorage.Plugin.Building;

using System;
using System.Collections.Generic;
using BaseStorage.Core.Abstractions;
using BaseStorage.Core.Availability;
using BaseStorage.Core.Consumption;
using BaseStorage.Core.Logging;
using BaseStorage.Core.Settings;
using BaseStorage.Plugin.Crafting;
using BaseStorage.Plugin.Patching;
using BaseStorage.Plugin.Reservations;

/// <summary>
/// The plugin-side entry point of build-from-base (contract build-from-base 5.4). It owns the one session, forwards the frame
/// tick and the patch bodies, and returns "run vanilla" whenever no session exists. Game-bound, so it is not linked into the
/// tests; its facts are proven by source scans.
/// </summary>
internal static class PluginBuilding
{
    /// <summary>The patch key used by the fail-closed wrapper of the per-frame tick.</summary>
    internal const string TickKey = "BaseStorage.Plugin.Building.PluginBuilding.Tick";

    private static GameBuildGame? game;
    private static ILogSink? logSink;

    /// <summary>Gets the session, or null until <see cref="Initialize"/> has run.</summary>
    internal static BuildSession? Current { get; private set; }

    /// <summary>Gets whether a build is past Idle (contract craft-from-base-toggle T6).</summary>
    internal static bool InFlight => Current != null && Current.Phase != CraftPhase.Idle;

    /// <summary>Creates the one session at boot. Later calls return the existing instance.</summary>
    /// <param name="reservations">The reservation driver.</param>
    /// <param name="clock">The clock.</param>
    /// <param name="settings">The settings.</param>
    /// <param name="sink">The log sink.</param>
    /// <returns>The session.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static BuildSession Initialize(ReservationDriver<Container> reservations, IClock clock, BaseStorageSettings settings, ILogSink sink)
    {
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
            var world = new GameCraftWorld();
            var layer = new CraftContainerLayer<Container>(world, reservations);
            var executor = new ConsumptionExecutor(layer, clock, settings);
            game = new GameBuildGame(world);
            logSink = sink;
            Current = new BuildSession(game, executor, sink);
        }

        return Current;
    }

    /// <summary>Advances the pending build by one frame, behind the fail-closed wrapper.</summary>
    internal static void Tick()
    {
        PatchSafety.Run(TickKey, static () => TickCore());
    }

    /// <summary>Answers the piece check from Core availability over the inventory and the base chests.</summary>
    /// <param name="player">The player.</param>
    /// <param name="piece">The piece.</param>
    /// <param name="mode">The requirement mode.</param>
    /// <param name="result">The result, set when this method answers.</param>
    /// <returns>False when the answer was set; true when vanilla runs.</returns>
    internal static bool OnHaveRequirements(Player player, Piece piece, Player.RequirementMode mode, ref bool result)
    {
        BuildSession? session = Current;
        GameBuildGame? gameSide = game;
        if (session == null || gameSide == null)
        {
            return true;
        }

        BuildGateReason reason = gameSide.EvaluateGate(player, piece, true, mode, out IReadOnlyList<CraftRequirement>? requirements);
        if (reason != BuildGateReason.UsesBase || requirements == null)
        {
            return true;
        }

        bool holds = gameSide.PreconditionsHold(player, piece);
        if (holds)
        {
            BaseSnapshot? snapshot = gameSide.GetSnapshot();
            if (snapshot == null)
            {
                return true;
            }

            holds = BaseAvailability.FindFirstMissing(snapshot, requirements) == null;
        }

        result = holds;
        return false;
    }

    /// <summary>Handles the game's placement attempt: starts a consume-before-place build or lets vanilla run.</summary>
    /// <param name="player">The player.</param>
    /// <param name="piece">The piece.</param>
    /// <param name="result">The result, set when vanilla must be skipped.</param>
    /// <returns>False when vanilla must be skipped; true when vanilla runs.</returns>
    internal static bool OnTryPlacePiece(Player player, Piece piece, ref bool result)
    {
        BuildSession? session = Current;
        GameBuildGame? gameSide = game;
        if (session == null || gameSide == null)
        {
            return true;
        }

        // Sets the BF8 guard before any game read, unless this is the commit call.
        session.EnterGuard();
        if (session.Phase == CraftPhase.Committing)
        {
            return session.OnTryPlacePiece(null, null);
        }

        BuildSelection? selection = null;
        ConsumptionPlan? plan = null;
        BuildGateReason reason = gameSide.EvaluateGate(player, piece, false, Player.RequirementMode.CanBuild, out IReadOnlyList<CraftRequirement>? requirements);
        if (reason == BuildGateReason.UsesBase && requirements != null)
        {
            gameSide.DropSnapshotMemo();
            BaseSnapshot? snapshot = gameSide.GetSnapshot();
            if (snapshot != null)
            {
                selection = new BuildSelection(piece);
                plan = ConsumptionPlanner.Plan(snapshot, requirements);
            }
            else
            {
                reason = BuildGateReason.Unreadable;
            }
        }

        if (selection == null)
        {
            Write(() => BuildMessages.Vanilla(reason));
        }

        bool run = session.OnTryPlacePiece(selection, plan);
        if (!run)
        {
            result = false;
        }

        return run;
    }

    /// <summary>Handles the game's piece creation: marks the commit and enforces the placement guard.</summary>
    /// <param name="player">The player.</param>
    /// <param name="piece">The piece.</param>
    /// <returns>False when the creation must be suppressed; true when vanilla runs.</returns>
    internal static bool OnPlacePiece(Player player, Piece piece)
    {
        BuildSession? session = Current;
        GameBuildGame? gameSide = game;
        if (session == null || gameSide == null || player != Player.m_localPlayer)
        {
            return true;
        }

        bool covers = true;
        if (session.Phase != CraftPhase.Committing && session.Guarded)
        {
            covers = gameSide.InventoryCovers(player, piece);
        }

        return session.OnPlacePiece(covers);
    }

    /// <summary>Handles the result of the game's placement attempt.</summary>
    /// <param name="placed">The result vanilla produced.</param>
    /// <returns>The result to keep.</returns>
    internal static bool AfterTryPlacePiece(bool placed)
    {
        BuildSession? session = Current;
        if (session == null)
        {
            return placed;
        }

        return session.AfterTryPlacePiece(placed);
    }

    private static void TickCore()
    {
        BuildSession? session = Current;
        if (session == null)
        {
            return;
        }

        bool active = session.Phase != CraftPhase.Idle;
        try
        {
            session.Tick();
        }
        finally
        {
            if (game != null && (active || session.Phase != CraftPhase.Idle))
            {
                game.DropMemo();
            }
        }
    }

    private static void Write(Func<string> line)
    {
        if (LogPolicy.IsEnabled(logSink, LogCategory.Detail))
        {
            LogPolicy.Write(logSink, LogCategory.Detail, line());
        }
    }
}
