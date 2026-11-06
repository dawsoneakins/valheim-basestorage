namespace BaseStorage.Plugin.Commands;

using BaseStorage.Plugin.Patching;
using BaseStorage.Shared;
using HarmonyLib;

/// <summary>
/// Postfix on Terminal.InitTerminal: registers the dons-qol console command once per process (CC7). The body is one fail-closed call.
/// </summary>
[HarmonyPatch(typeof(Terminal), PatchTargetNames.Terminal.InitTerminal)]
internal static class TerminalInitTerminalPatch
{
    /// <summary>The patch key used by the fail-closed wrapper to report a failure once per session.</summary>
    internal const string PatchKey = "BaseStorage.Plugin.Commands.TerminalInitTerminalPatch.Postfix";

    private static void Postfix() => PatchSafety.Run(PatchKey, static () => ConsoleCommands.Register());
}
