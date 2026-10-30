namespace BaseStorage.PatchTargets.Tests.Infrastructure;

using System;

/// <summary>
/// Pure skip decision (contract 4.8.2). The checks run in order: folder, game DLL, Plugin DLL (only when
/// <paramref name="needsPlugin"/>). Returns null or the exact BST2002, BST2003 or BST2004 text.
/// </summary>
internal static class SkipGate
{
    private const string Tail = " Set VALHEIM_DIR or create Directory.Build.user.props with <ValheimDir>, then rebuild the tests.";

    public static string? Decide(
        string valheimDir,
        string gameDll,
        string pluginDll,
        string configuration,
        bool needsPlugin,
        Func<string, bool> directoryExists,
        Func<string, bool> fileExists)
    {
        if (!directoryExists(valheimDir))
        {
            return "BST2002: Skipped: ValheimDir '" + valheimDir + "' does not exist." + Tail;
        }

        if (!fileExists(gameDll))
        {
            return "BST2003: Skipped: '" + gameDll + "' does not exist." + Tail;
        }

        if (needsPlugin && !fileExists(pluginDll))
        {
            return "BST2004: Skipped: Plugin assembly '" + pluginDll + "' does not exist. Build BaseStorage.sln -c " + configuration + " first.";
        }

        return null;
    }

    /// <summary>The skip decision for the current build: BuildValues plus the real file system.</summary>
    internal static string? ForBuild(bool needsPlugin) =>
        Decide(
            BuildValues.ValheimDir,
            BuildValues.GameDll,
            BuildValues.PluginAssemblyPath,
            BuildValues.Configuration,
            needsPlugin,
            System.IO.Directory.Exists,
            System.IO.File.Exists);
}
