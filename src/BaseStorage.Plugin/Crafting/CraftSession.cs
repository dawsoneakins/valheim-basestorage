namespace BaseStorage.Plugin.Crafting;

using System;
using BaseStorage.Core.Abstractions;
using BaseStorage.Core.Availability;
using BaseStorage.Core.Consumption;
using BaseStorage.Core.Logging;
using BaseStorage.Core.Ui;

/// <summary>
/// The craft session of craft-from-base (contract craft-from-base 5.3): consume before create. It skips vanilla crafting, runs the
/// atomic executor once per frame, and only after success lets the game create the item (the commit). Every other end consumes
/// nothing and shows one notice, except an abandoned craft, which is silent. Game-free.
/// </summary>
internal sealed class CraftSession
{
    private readonly ICraftGame game;
    private readonly ConsumptionExecutor executor;
    private readonly ILogSink? sink;

    /// <summary>Creates the session; reads nothing.</summary>
    /// <param name="game">The game port.</param>
    /// <param name="executor">The consumption executor.</param>
    /// <param name="sink">The log sink, or null.</param>
    /// <exception cref="ArgumentNullException"><paramref name="game"/> or <paramref name="executor"/> is null.</exception>
    internal CraftSession(ICraftGame game, ConsumptionExecutor executor, ILogSink? sink)
    {
        this.game = game ?? throw new ArgumentNullException(nameof(game));
        this.executor = executor ?? throw new ArgumentNullException(nameof(executor));
        this.sink = sink;
    }

    /// <summary>Gets the phase.</summary>
    internal CraftPhase Phase { get; private set; }

    /// <summary>Gets the pending selection; non-null exactly while the phase is not idle.</summary>
    internal CraftSelection? Selection { get; private set; }

    /// <summary>Gets the pending execution; non-null exactly while the phase is not idle.</summary>
    internal ConsumptionExecution? Execution { get; private set; }

    /// <summary>Gets the number of handled craft attempts.</summary>
    internal int Token { get; private set; }

    /// <summary>Gets a value indicating whether the commit's resource consumption was reached.</summary>
    internal bool ItemCreated { get; private set; }

    /// <summary>Gets a value indicating whether a vanilla crafting body runs that the mod did not commit (CF8).</summary>
    internal bool VanillaCraftRunning { get; private set; }

    /// <summary>Gets how the last handled craft ended.</summary>
    internal CraftEnd? LastEnd { get; private set; }

    /// <summary>Gets the notice key for an end.</summary>
    /// <param name="end">How the craft ended.</param>
    /// <returns>The string key, or null when no notice is shown.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="end"/> is not a defined value.</exception>
    internal static string? NoticeKeyFor(CraftEnd end)
    {
        switch (end)
        {
            case CraftEnd.Returned:
            case CraftEnd.Failed:
            case CraftEnd.PreFlightFailed:
            case CraftEnd.Unavailable:
                return StringKeys.MessageCraftMaterialsMoved;
            case CraftEnd.Created:
                return StringKeys.MessageCraftUsedBase;
            case CraftEnd.Abandoned:
                return null;
            default:
                throw new ArgumentOutOfRangeException(nameof(end));
        }
    }

    /// <summary>Tells whether the materials check should answer true because the materials are already taken.</summary>
    /// <param name="recipe">The recipe being checked.</param>
    /// <returns>True during the commit of this recipe, before the item is created.</returns>
    internal bool CommitAnswer(object recipe) =>
        Phase == CraftPhase.Committing
        && !ItemCreated
        && Selection.HasValue
        && ReferenceEquals(recipe, Selection.Value.Recipe);

    /// <summary>Tells whether the materials check should count chests.</summary>
    /// <param name="recipe">The recipe being checked.</param>
    /// <returns>False during a commit of this recipe or while a vanilla craft runs; otherwise true.</returns>
    internal bool CountsChests(object recipe) => !CommitAnswer(recipe) && !VanillaCraftRunning;

    /// <summary>Handles a call of the game's crafting method.</summary>
    /// <param name="selection">The selection, or null when the gate is closed.</param>
    /// <param name="plan">The consumption plan, or null when the materials are not there.</param>
    /// <returns>True when vanilla should run.</returns>
    internal bool OnDoCrafting(CraftSelection? selection, ConsumptionPlan? plan)
    {
        if (Phase == CraftPhase.Committing)
        {
            return true;
        }

        VanillaCraftRunning = true;
        if (!selection.HasValue)
        {
            return true;
        }

        CraftSelection chosen = selection.Value;
        if (Phase == CraftPhase.Executing)
        {
            if (Selection.HasValue && Selection.Value == chosen)
            {
                VanillaCraftRunning = false;
                return false;
            }

            Execution?.Cancel();
            End(CraftEnd.Abandoned);
        }

        Token++;
        if (plan is null)
        {
            End(CraftEnd.Unavailable);
            VanillaCraftRunning = false;
            return false;
        }

        if (plan.ExpectedContainers.Count == 0)
        {
            return true;
        }

        if (!game.PreFlight(chosen))
        {
            End(CraftEnd.PreFlightFailed);
            VanillaCraftRunning = false;
            return false;
        }

        game.ForgetTaken();
        Execution = executor.Execute(plan);
        Selection = chosen;
        ItemCreated = false;
        Phase = CraftPhase.Executing;
        Write(() => CraftMessages.Started(Token, plan.Takes.Count, plan.ExpectedContainers.Count));
        VanillaCraftRunning = false;
        return false;
    }

    /// <summary>Handles a call of the game's resource consumption.</summary>
    /// <param name="isCommittedRecipeResources">True when the call is for the committed recipe's resources.</param>
    /// <returns>True when vanilla should run.</returns>
    internal bool OnConsumeResources(bool isCommittedRecipeResources)
    {
        if (Phase == CraftPhase.Committing && isCommittedRecipeResources)
        {
            ItemCreated = true;
            return false;
        }

        return true;
    }

    /// <summary>Advances the pending craft by one frame.</summary>
    internal void Tick()
    {
        VanillaCraftRunning = false;
        if (Phase != CraftPhase.Executing)
        {
            return;
        }

        ConsumptionExecution execution = Execution!;
        CraftSelection selection = Selection!.Value;
        ConsumptionResult? result = execution.Result;
        if (result is null)
        {
            if (!game.IsStillSelected(selection))
            {
                execution.Cancel();
                End(CraftEnd.Abandoned);
                return;
            }

            if (!game.PreFlight(selection))
            {
                execution.Cancel();
                End(CraftEnd.PreFlightFailed);
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
            Write(() => CraftMessages.Failed(Token, failed.Outcome, failed.FailedContainer));
            End(CraftEnd.Failed, false);
            return;
        }

        Phase = CraftPhase.Committing;
        ItemCreated = false;
        try
        {
            game.Commit(selection);
        }
        finally
        {
            if (ItemCreated)
            {
                game.ForgetTaken();
                End(CraftEnd.Created);
            }
            else
            {
                int stacks = game.ReturnTaken();
                Write(() => CraftMessages.Returned(Token, stacks));
                End(CraftEnd.Returned, false);
            }
        }
    }

    private void End(CraftEnd end, bool logEnd = true)
    {
        if (logEnd)
        {
            Write(() => CraftMessages.Ended(Token, end));
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
