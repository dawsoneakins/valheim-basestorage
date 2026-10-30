namespace BaseStorage.Plugin.Commands;

using BaseStorage.Core.Bases.Resolution;
using BaseStorage.Core.Commands;
using BaseStorage.Core.Organize.Run;
using BaseStorage.Core.Snapshots;
using BaseStorage.Plugin.Organize;
using BaseStorage.Plugin.Patching;
using UnityEngine;

/// <summary>
/// The basestorage console command (contract section 4): registers it with the vanilla terminal and forwards a typed line to
/// <see cref="OrganizeRunner.StartAt"/>. Core parses the line and chooses the one reply; this type only reads the player position and prints.
/// </summary>
internal static class ConsoleCommands
{
    /// <summary>The patch key used by the fail-closed wrapper to report a handler failure once per session.</summary>
    internal const string CommandKey = "BaseStorage.Plugin.Commands.ConsoleCommands.Run";

    /// <summary>Gets a value indicating whether the command was registered (true after the first <see cref="Register"/>).</summary>
    internal static bool IsRegistered { get; private set; }

    /// <summary>Registers the command with the vanilla terminal; a second call registers nothing.</summary>
    internal static void Register()
    {
        if (IsRegistered)
        {
            return;
        }

        new Terminal.ConsoleCommand(ConsoleCommandText.CommandName, ConsoleCommandText.Description(), new Terminal.ConsoleEvent(OnCommand));
        IsRegistered = true;
    }

    /// <summary>The command handler: runs the command and lets nothing escape into the terminal.</summary>
    /// <param name="args">The typed line, split on spaces, and the terminal that ran it.</param>
    internal static void OnCommand(Terminal.ConsoleEventArgs args) =>
        PatchSafety.Run(CommandKey, args, static a => Run(a));

    private static void Run(Terminal.ConsoleEventArgs args)
    {
        if (args == null)
        {
            return;
        }

        string line;
        if (ConsoleCommandText.Parse(args.Args) == ConsoleCommandRequest.Usage)
        {
            line = ConsoleCommandText.Usage();
        }
        else
        {
            OrganizeStartOutcome outcome;
            Player player = Player.m_localPlayer;
            if (player == null)
            {
                outcome = OrganizeStartOutcome.NoPlayer;
            }
            else
            {
                Vector3 at = player.transform.position;
                outcome = OrganizeRunner.StartAt(BaseStart.FromPosition(new WorldPosition(at.x, at.y, at.z)));
            }

            OrganizeRunCoordinator? coordinator = OrganizeRunner.Current;
            line = ConsoleCommandText.Reply(outcome, coordinator?.State ?? OrganizeRunState.Idle, coordinator?.RefusedBy);
        }

        if (args.Context != null)
        {
            args.Context.AddString(line);
        }
    }
}
