namespace BaseStorage.Plugin.Crafting;

using System;
using System.Collections.Generic;
using BaseStorage.Core.Abstractions;
using BaseStorage.Core.Availability;
using BaseStorage.Core.Consumption;
using BaseStorage.Core.Logging;
using BaseStorage.Core.Settings;
using BaseStorage.Plugin.Patching;
using BaseStorage.Plugin.Reservations;

/// <summary>
/// The plugin-side entry point of craft-from-base (contract craft-from-base 5.3). It owns the one session, forwards the frame
/// tick and the three patch bodies, and returns "run vanilla" whenever no session exists. Game-bound, so it is not linked into
/// the tests; its facts are proven by source scans.
/// </summary>
internal static class PluginCrafting
{
    /// <summary>The patch key used by the fail-closed wrapper of the per-frame tick.</summary>
    internal const string TickKey = "BaseStorage.Plugin.Crafting.PluginCrafting.Tick";

    private static GameCraftGame? game;
    private static ILogSink? logSink;

    /// <summary>Gets the session, or null until <see cref="Initialize"/> has run.</summary>
    internal static CraftSession? Current { get; private set; }

    /// <summary>Gets whether a craft is past Idle (contract craft-from-base-toggle T6).</summary>
    internal static bool InFlight => Current != null && Current.Phase != CraftPhase.Idle;

    /// <summary>Creates the one session at boot. Later calls return the existing instance.</summary>
    /// <param name="reservations">The reservation driver.</param>
    /// <param name="clock">The clock.</param>
    /// <param name="settings">The settings.</param>
    /// <param name="sink">The log sink.</param>
    /// <returns>The session.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static CraftSession Initialize(ReservationDriver<Container> reservations, IClock clock, BaseStorageSettings settings, ILogSink sink)
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
            game = new GameCraftGame(world);
            logSink = sink;
            Current = new CraftSession(game, executor, sink);
        }

        return Current;
    }

    /// <summary>Advances the pending craft by one frame, behind the fail-closed wrapper.</summary>
    internal static void Tick()
    {
        PatchSafety.Run(TickKey, static () => TickCore());
    }

    /// <summary>Answers the materials half of the recipe check from Core availability.</summary>
    /// <param name="player">The player.</param>
    /// <param name="recipe">The recipe.</param>
    /// <param name="discover">The discover argument.</param>
    /// <param name="qualityLevel">The quality level.</param>
    /// <param name="amount">The multiplier.</param>
    /// <param name="result">The result, set when this method answers.</param>
    /// <returns>False when the answer was set; true when vanilla runs.</returns>
    internal static bool OnHaveRequirementItems(Player player, Recipe recipe, bool discover, int qualityLevel, int amount, ref bool result)
    {
        CraftSession? session = Current;
        GameCraftGame? gameSide = game;
        if (session == null || gameSide == null)
        {
            return true;
        }

        if (session.CommitAnswer(recipe))
        {
            result = true;
            return false;
        }

        if (!session.CountsChests(recipe))
        {
            return true;
        }

        CraftGateReason reason = gameSide.EvaluateGate(player, recipe, discover, qualityLevel, amount, out CraftingStation? station, out IReadOnlyList<CraftRequirement>? requirements);
        if (reason != CraftGateReason.UsesBase || station == null || requirements == null)
        {
            return true;
        }

        BaseSnapshot? snapshot = gameSide.GetSnapshot(station);
        if (snapshot == null)
        {
            return true;
        }

        result = BaseAvailability.FindFirstMissing(snapshot, requirements) == null;
        return false;
    }

    /// <summary>Handles the game's crafting method: starts a consume-before-create craft or lets vanilla run.</summary>
    /// <param name="gui">The crafting panel.</param>
    /// <param name="player">The player.</param>
    /// <returns>False when vanilla must be skipped; true when vanilla runs.</returns>
    internal static bool OnDoCrafting(InventoryGui gui, Player player)
    {
        CraftSession? session = Current;
        GameCraftGame? gameSide = game;
        if (session == null || gameSide == null)
        {
            return true;
        }

        // Sets the CF8 marker before any game read, unless this is the commit call.
        session.OnDoCrafting(null, null);
        if (session.Phase == CraftPhase.Committing)
        {
            return true;
        }

        CraftSelection? selection = null;
        ConsumptionPlan? plan = null;
        CraftGateReason reason = CraftGateReason.NoStation;
        CraftingStation? station = player != null ? player.GetCurrentCraftingStation() : null;
        if (station != null && gameSide.TryReadSelection(gui, station, out CraftSelection read))
        {
            gameSide.DropMemo();
            Recipe recipe = (Recipe)read.Recipe;
            reason = gameSide.EvaluateGate(player!, recipe, false, read.QualityLevel, read.Multiplier, out CraftingStation? gateStation, out IReadOnlyList<CraftRequirement>? requirements);
            if (reason == CraftGateReason.UsesBase && gateStation != null && requirements != null)
            {
                BaseSnapshot? snapshot = gameSide.GetSnapshot(gateStation);
                if (snapshot != null)
                {
                    selection = read;
                    plan = ConsumptionPlanner.Plan(snapshot, requirements);
                }
                else
                {
                    reason = CraftGateReason.Unreadable;
                }
            }
        }

        if (selection == null)
        {
            Write(() => CraftMessages.Vanilla(reason));
        }

        return session.OnDoCrafting(selection, plan);
    }

    /// <summary>Handles the game's resource consumption: skips vanilla only for the committed craft.</summary>
    /// <param name="player">The player.</param>
    /// <param name="requirements">The requirement array the game consumes.</param>
    /// <returns>False when vanilla must be skipped; true when vanilla runs.</returns>
    internal static bool OnConsumeResources(Player player, Piece.Requirement[] requirements)
    {
        CraftSession? session = Current;
        if (session == null)
        {
            return true;
        }

        CraftSelection? selection = session.Selection;
        bool committed = selection != null && ReferenceEquals(requirements, ((Recipe)selection.Value.Recipe).m_resources);
        return session.OnConsumeResources(committed);
    }

    private static void TickCore()
    {
        CraftSession? session = Current;
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
