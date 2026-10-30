namespace BaseStorage.Core.Commands;

using System;
using System.Collections.Generic;
using BaseStorage.Core.Organize.Run;
using BaseStorage.Core.Ui;

/// <summary>The pure decisions of the basestorage console command: argument parsing and the one reply line (contract section 2).</summary>
public static class ConsoleCommandText
{
    /// <summary>The console command word, as registered (vanilla lower-cases the typed word before lookup).</summary>
    public const string CommandName = "basestorage";

    /// <summary>The one sub-command word.</summary>
    public const string OrganizeVerb = "organize";

    /// <summary>Parses the argument list the game passes, where the first entry is the command word as typed.</summary>
    /// <param name="args">The arguments; null, empty and whitespace-only tokens after the first are ignored.</param>
    /// <returns><see cref="ConsoleCommandRequest.Organize"/> when exactly one token is left and it is the organize verb; otherwise <see cref="ConsoleCommandRequest.Usage"/>.</returns>
    public static ConsoleCommandRequest Parse(IReadOnlyList<string?>? args)
    {
        if (args is null)
        {
            return ConsoleCommandRequest.Usage;
        }

        string? only = null;
        for (int i = 1; i < args.Count; i++)
        {
            string? token = args[i]?.Trim();
            if (string.IsNullOrEmpty(token))
            {
                continue;
            }

            if (only is not null)
            {
                return ConsoleCommandRequest.Usage;
            }

            only = token;
        }

        return only is not null && string.Equals(only, OrganizeVerb, StringComparison.OrdinalIgnoreCase)
            ? ConsoleCommandRequest.Organize
            : ConsoleCommandRequest.Usage;
    }

    /// <summary>Gets the strings-table key of the reply to an Organize start.</summary>
    /// <param name="outcome">What <c>Start</c> returned.</param>
    /// <param name="stateAfter">The coordinator state read right after the start, or Idle when there is no coordinator.</param>
    /// <returns>The strings-table key.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="outcome"/> is not defined; ParamName is outcome.</exception>
    public static string ReplyKey(OrganizeStartOutcome outcome, OrganizeRunState stateAfter)
    {
        switch (outcome)
        {
            case OrganizeStartOutcome.Started:
                return stateAfter switch
                {
                    OrganizeRunState.Previewing => StringKeys.ConsoleOrganizePreview,
                    OrganizeRunState.Running => StringKeys.ConsoleOrganizeStarted,
                    _ => StringKeys.MessageOrganizeCancelled,
                };
            case OrganizeStartOutcome.AlreadyActive:
            case OrganizeStartOutcome.LockRefused:
                return StringKeys.MessageBaseInProgress;
            case OrganizeStartOutcome.NoPlayer:
                return StringKeys.ConsoleOrganizeNoPlayer;
            case OrganizeStartOutcome.NotInBase:
                return StringKeys.MessageBaseNotInBase;
            case OrganizeStartOutcome.NoChests:
                return StringKeys.StationActionsDisabledNoChests;
            default:
                throw new ArgumentOutOfRangeException(nameof(outcome), outcome, "The outcome is not defined.");
        }
    }

    /// <summary>Gets the one reply line, on a single line.</summary>
    /// <param name="outcome">What <c>Start</c> returned.</param>
    /// <param name="stateAfter">The coordinator state read right after the start, or Idle when there is no coordinator.</param>
    /// <param name="holderName">The holder named by an in-progress refusal; ignored for every other outcome.</param>
    /// <returns>The reply line.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="outcome"/> is not defined; ParamName is outcome.</exception>
    public static string Reply(OrganizeStartOutcome outcome, OrganizeRunState stateAfter, string? holderName)
    {
        string key = ReplyKey(outcome, stateAfter);
        StringTable table = StringTable.English;
        if (outcome == OrganizeStartOutcome.AlreadyActive || outcome == OrganizeStartOutcome.LockRefused)
        {
            string holder = string.IsNullOrWhiteSpace(holderName) ? table.Get(StringKeys.PlayerUnknown) : holderName!;
            return SingleLine.Of(table.Format(key, holder));
        }

        return SingleLine.Of(table.Format(key));
    }

    /// <summary>Gets the usage line.</summary>
    /// <returns>The usage line.</returns>
    public static string Usage() => StringTable.English.Get(StringKeys.ConsoleBasestorageUsage);

    /// <summary>Gets the command description shown by the vanilla help command.</summary>
    /// <returns>The description.</returns>
    public static string Description() => StringTable.English.Get(StringKeys.ConsoleBasestorageDescription);
}
