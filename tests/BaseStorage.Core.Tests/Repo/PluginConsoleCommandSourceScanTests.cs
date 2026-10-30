namespace BaseStorage.Core.Tests.Repo;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

/// <summary>
/// GW-U-2320 to GW-U-2323 (AC-1, AC-2, AC-3, AC-4; organize-console-command contract 4.4 CS1-CS7): the Plugin console folder is the two
/// contracted files, the registration is a one-call postfix on Terminal.InitTerminal, the one hand-off to the run is the only path
/// that starts an Organize, and no file outside the folder names the console types. Comment-stripped scans of src/BaseStorage.Plugin.
/// </summary>
[Trait("Category", "Repo")]
public sealed class PluginConsoleCommandSourceScanTests
{
    private const string Folder = "src/BaseStorage.Plugin/Commands/";
    private const string Commands = Folder + "ConsoleCommands.cs";
    private const string Patch = Folder + "TerminalInitTerminalPatch.cs";
    private const string CommandKey = "BaseStorage.Plugin.Commands.ConsoleCommands.Run";
    private const string PatchKey = "BaseStorage.Plugin.Commands.TerminalInitTerminalPatch.Postfix";

    private static string[] FolderFiles(Dictionary<string, string> sources) =>
        sources.Keys.Where(key => key.StartsWith(Folder, StringComparison.Ordinal)).OrderBy(key => key, StringComparer.Ordinal).ToArray();

    // GW-U-2320 (CS4): ConsoleCommands.cs holds the one hand-off, the reply and the registration of the run path.
    [Fact]
    public void ConsoleCommandsHoldsTheOneHandOffAndThePrintedReply()
    {
        string code = PluginSourceScan.Sources()[Commands];

        Assert.Contains("PatchSafety.Run(CommandKey, args,", code, StringComparison.Ordinal);
        Assert.Contains("ConsoleCommandText.Parse(args.Args)", code, StringComparison.Ordinal);
        Assert.Contains("ConsoleCommandText.Usage()", code, StringComparison.Ordinal);
        Assert.Contains("Player.m_localPlayer", code, StringComparison.Ordinal);
        Assert.Contains("OrganizeRunner.StartAt(BaseStart.FromPosition(new WorldPosition(", code, StringComparison.Ordinal);
        Assert.Contains("ConsoleCommandText.Reply(", code, StringComparison.Ordinal);
        Assert.Contains(".RefusedBy", code, StringComparison.Ordinal);
        Assert.Contains(".AddString(line)", code, StringComparison.Ordinal);
        Assert.Contains("internal const string CommandKey = \"" + CommandKey + "\";", code, StringComparison.Ordinal);
    }

    // GW-U-2321 (CS5, CS6, CS7): the folder starts no Organize of its own, takes no lock, shows no popup and names no lock or planner
    // type. Its only literals are the two patch keys, and the console types appear nowhere else in the Plugin.
    [Fact]
    public void TheFolderHasNoSidePathAndTheConsoleTypesStayInIt()
    {
        Dictionary<string, string> sources = PluginSourceScan.Sources();
        string[] banned =
        {
            ".Start(", "StartDeposit", "OrganizeLock", "GameOrganizeLockStore", "IOrganizeLockStore", "OrganizePreviewPopup", "UnifiedPopup",
            "Notices.Show(", ".ShowMessage(", ".Resolve(", "OrganizePlanner", "PersistenceKeys", "ZInput", "FromStation(",
        };

        foreach (string file in FolderFiles(sources))
        {
            foreach (string token in banned)
            {
                Assert.DoesNotContain(token, sources[file], StringComparison.Ordinal);
            }

            string[] literals = Regex.Matches(sources[file], "\"([^\"\\n]*)\"").Select(m => m.Groups[1].Value).OrderBy(v => v, StringComparer.Ordinal).ToArray();
            Assert.True(literals.All(value => value == CommandKey || value == PatchKey), file + " holds a literal other than the two patch keys.");
        }

        Assert.Equal(new[] { Commands }, PluginSourceScan.FilesMatching(sources, @"\.AddString\("));
        string[] outside = sources.Keys
            .Where(key => !key.StartsWith(Folder, StringComparison.Ordinal) && Regex.IsMatch(sources[key], @"ConsoleCommandText|Terminal\.ConsoleCommand\b"))
            .ToArray();
        Assert.Empty(outside);
    }

    // GW-U-2322 (CS1, CS2): the folder holds exactly the two files, and the one registration patch has the one-call postfix body.
    [Fact]
    public void TheRegistrationPatchHasTheOneCallPostfixShape()
    {
        Dictionary<string, string> sources = PluginSourceScan.Sources();

        Assert.Equal(new[] { Commands, Patch }, FolderFiles(sources));
        Assert.Equal(1, PluginSourceScan.CountMatches(sources, @"\[HarmonyPatch\(typeof\(Terminal\)"));

        string text = sources[Patch];
        Assert.Contains("[HarmonyPatch(typeof(Terminal), PatchTargetNames.Terminal.InitTerminal)]", text, StringComparison.Ordinal);
        Assert.Matches(@"private\s+static\s+void\s+Postfix\s*\(\s*\)\s*=>\s*PatchSafety\.Run\s*\(\s*PatchKey\s*,\s*static\s*\(\s*\)\s*=>\s*ConsoleCommands\.Register\s*\(\s*\)\s*\)\s*;", text);
        Assert.Contains("internal const string PatchKey = \"" + PatchKey + "\";", text, StringComparison.Ordinal);
    }

    // GW-U-2323 (CS3, CC5, CC7): the command is registered once, with the three arguments and no flags, behind an idempotent Register.
    [Fact]
    public void TheRegistrationCallAppearsOnceWithNoFlags()
    {
        Dictionary<string, string> sources = PluginSourceScan.Sources();

        Assert.Equal(1, PluginSourceScan.CountMatches(sources, Regex.Escape("new Terminal.ConsoleCommand(")));
        Assert.Equal(new[] { Commands }, PluginSourceScan.FilesMatching(sources, Regex.Escape("new Terminal.ConsoleCommand(")));

        string code = Regex.Replace(sources[Commands], @"\s+", string.Empty);
        Assert.Contains("newTerminal.ConsoleCommand(ConsoleCommandText.CommandName,ConsoleCommandText.Description(),newTerminal.ConsoleEvent(OnCommand))", code, StringComparison.Ordinal);
        Assert.Matches(@"internal\s+static\s+bool\s+IsRegistered\s*\{\s*get;\s*private\s+set;\s*\}", sources[Commands]);
        Assert.Matches(@"if\s*\(\s*IsRegistered\s*\)\s*(\{\s*)?return\s*;", sources[Commands]);
    }
}
