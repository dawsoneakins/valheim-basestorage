namespace BaseStorage.Plugin.Building;

using System;
using BaseStorage.Core.Abstractions;
using BaseStorage.Core.Availability;
using BaseStorage.Core.Consumption;
using BaseStorage.Core.Logging;
using BaseStorage.Core.Ui;
using BaseStorage.Plugin.Crafting;

/// <summary>
/// The build session of build-from-base (contract build-from-base 5.4): consume before place. It skips vanilla placement, runs
/// the atomic executor once per frame, and only after success lets the game create the piece (the commit). The guard keeps a
/// failing patch from ever placing a piece for less than its cost. Game-free.
/// </summary>
internal sealed class BuildSession
{
    private readonly IBuildGame game;
    private readonly ConsumptionExecutor executor;
    private readonly ILogSink? sink;

    /// <summary>Creates the session; reads nothing.</summary>
    /// <param name="game">The game port.</param>
    /// <param name="executor">The consumption executor.</param>
    /// <param name="sink">The log sink, or null.</param>
    /// <exception cref="ArgumentNullException"><paramref name="game"/> or <paramref name="executor"/> is null.</exception>
    internal BuildSession(IBuildGame game, ConsumptionExecutor executor, ILogSink? sink)
    {
        this.game = game ?? throw new ArgumentNullException(nameof(game));
        this.executor = executor ?? throw new ArgumentNullException(nameof(executor));
        this.sink = sink;
    }

    /// <summary>Gets the phase.</summary>
    internal CraftPhase Phase { get; private set; }

    /// <summary>Gets the pending selection; non-null exactly while the phase is not idle.</summary>
    internal BuildSelection? Selection { get; private set; }

    /// <summary>Gets the pending execution; non-null exactly while the phase is not idle.</summary>
    internal ConsumptionExecution? Execution { get; private set; }

    /// <summary>Gets the number of handled place attempts.</summary>
    internal int Token { get; private set; }

    /// <summary>Gets a value indicating whether the game created a piece during the commit.</summary>
    internal bool PieceCreated { get; private set; }

    /// <summary>Gets a value indicating whether the placement guard is set (BF8).</summary>
    internal bool Guarded { get; private set; }

    /// <summary>Gets a value indicating whether a piece creation was suppressed while guarded.</summary>
    internal bool Suppressed { get; private set; }

    /// <summary>Gets how the last handled build ended.</summary>
    internal BuildEnd? LastEnd { get; private set; }

    /// <summary>Gets the notice key for an end.</summary>
    /// <param name="end">How the build ended.</param>
    /// <returns>The string key, or null when no notice is shown.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="end"/> is not a defined value.</exception>
    internal static string? NoticeKeyFor(BuildEnd end)
    {
        switch (end)
        {
            case BuildEnd.Failed:
            case BuildEnd.PreFlightFailed:
            case BuildEnd.Unavailable:
                return StringKeys.MessageCraftMaterialsMoved;
            case BuildEnd.Placed:
                return StringKeys.MessageCraftUsedBase;
            case BuildEnd.Returned:
            case BuildEnd.Abandoned:
            case BuildEnd.PlacementRefused:
                return null;
            default:
                throw new ArgumentOutOfRangeException(nameof(end));
        }
    }

    /// <summary>Sets the placement guard; the first statement of the placement prefix.</summary>
    internal void EnterGuard()
    {
        if (Phase == CraftPhase.Committing)
        {
            return;
        }

        Guarded = true;
        Suppressed = false;
    }

    /// <summary>Handles a call of the game's placement attempt.</summary>
    /// <param name="selection">The selection, or null when the gate is closed.</param>
    /// <param name="plan">The consumption plan, or null when the materials are not there.</param>
    /// <returns>True when vanilla should run.</returns>
    internal bool OnTryPlacePiece(BuildSelection? selection, ConsumptionPlan? plan)
    {
        if (Phase == CraftPhase.Committing)
        {
            return true;
        }

        if (!selection.HasValue)
        {
            Guarded = false;
            return true;
        }

        BuildSelection chosen = selection.Value;
        if (Phase == CraftPhase.Executing)
        {
            if (Selection.HasValue && Selection.Value == chosen)
            {
                Guarded = false;
                return false;
            }

            Execution?.Cancel();
            End(BuildEnd.Abandoned);
        }

        Token++;
        if (plan is null)
        {
            End(BuildEnd.Unavailable);
            Guarded = false;
            return false;
        }

        if (plan.ExpectedContainers.Count == 0)
        {
            Guarded = false;
            return true;
        }

        if (!game.PreFlight(chosen))
        {
            End(BuildEnd.PlacementRefused);
            return true;
        }

        game.ForgetTaken();
        Execution = executor.Execute(plan);
        Selection = chosen;
        Phase = CraftPhase.Executing;
        Write(() => BuildMessages.Started(Token, plan.Takes.Count, plan.ExpectedContainers.Count));
        Guarded = false;
        return false;
    }

    /// <summary>Handles a call of the game's piece creation.</summary>
    /// <param name="inventoryCovers">True when the inventory alone covers the piece by the game's own rule.</param>
    /// <returns>True when vanilla should run.</returns>
    internal bool OnPlacePiece(bool inventoryCovers)
    {
        if (Phase == CraftPhase.Committing)
        {
            PieceCreated = true;
            return true;
        }

        if (!Guarded || inventoryCovers)
        {
            return true;
        }

        Suppressed = true;
        Write(BuildMessages.Suppressed);
        return false;
    }

    /// <summary>Handles the result of the game's placement attempt.</summary>
    /// <param name="placed">The result vanilla produced.</param>
    /// <returns>The result to keep.</returns>
    internal bool AfterTryPlacePiece(bool placed)
    {
        if (Phase == CraftPhase.Committing)
        {
            return placed;
        }

        Guarded = false;
        if (Suppressed)
        {
            Suppressed = false;
            return false;
        }

        return placed;
    }

    /// <summary>Advances the pending build by one frame.</summary>
    internal void Tick()
    {
        Guarded = false;
        Suppressed = false;
        if (Phase != CraftPhase.Executing)
        {
            return;
        }

        ConsumptionExecution execution = Execution!;
        BuildSelection selection = Selection!.Value;
        ConsumptionResult? result = execution.Result;
        if (result is null)
        {
            if (!game.IsStillSelected(selection))
            {
                execution.Cancel();
                End(BuildEnd.Abandoned);
                return;
            }

            if (!game.PreFlight(selection))
            {
                execution.Cancel();
                End(BuildEnd.PreFlightFailed);
                return;
            }

            result = execution.Poll();
            if (result is null)
            {
                return;
            }
        }

        if (!result.IsSuccess)
        {
            ConsumptionResult failed = result;
            Write(() => BuildMessages.Failed(Token, failed.Outcome, failed.FailedContainer));
            End(BuildEnd.Failed, false);
            return;
        }

        Phase = CraftPhase.Committing;
        PieceCreated = false;
        try
        {
            game.Commit(selection);
        }
        finally
        {
            if (PieceCreated)
            {
                game.ForgetTaken();
                End(BuildEnd.Placed);
            }
            else
            {
                int stacks = game.ReturnTaken();
                Write(() => BuildMessages.Returned(Token, stacks));
                End(BuildEnd.Returned, false);
            }
        }
    }

    private void End(BuildEnd end, bool logEnd = true)
    {
        if (logEnd)
        {
            Write(() => BuildMessages.Ended(Token, end));
        }

        LastEnd = end;
        string? key = NoticeKeyFor(end);
        Phase = CraftPhase.Idle;
        Selection = null;
        Execution = null;
        if (key is not null)
        {
            game.ShowNotice(key);
        }
    }

    private void Write(Func<string> line)
    {
        if (LogPolicy.IsEnabled(sink, LogCategory.Detail))
        {
            LogPolicy.Write(sink, LogCategory.Detail, line());
        }
    }
}
