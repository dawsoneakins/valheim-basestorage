namespace BaseStorage.Core.Tests.Repo;

using System;
using System.Linq;

/// <summary>
/// GW-U-1824 (AC-6; contract craft-from-base-toggle 10): package/README.md documents CraftFromBase in the Configuration section,
/// directly after the DebugLogging paragraph and before Categories file, exactly once.
/// </summary>
public sealed class ReadmeCraftFromBaseTests
{
    private const string Readme = "package/README.md";
    private const string Paragraph = "`CraftFromBase` (under `[General]`, default `true`) lets crafting, upgrading, crafting several at once and building at a station inside a base use the materials in the base chests, and shows the chest amounts in the requirement lists. Set it to `false` to turn all of that off: crafting and building are exactly vanilla and no chest is read or reserved. It is host-synced: in multiplayer the host's value is used, and a friend gets it when joining. A change takes effect at the next craft or build without a restart.";

    // GW-U-1824 (AC-6): the exact paragraph appears once, inside Configuration, after the DebugLogging paragraph.
    [Fact]
    public void TheParagraphIsInConfigurationOnceDirectlyAfterDebugLogging()
    {
        string[] lines = RepoRoot.ReadText(Readme).Split('\n');
        int configuration = Array.FindIndex(lines, line => line == "## Configuration");
        int categories = Array.FindIndex(lines, line => line == "## Categories file");
        int debugLogging = Array.FindIndex(lines, line => line.Contains("`DebugLogging`", StringComparison.Ordinal));
        int paragraph = Array.FindIndex(lines, line => line == Paragraph);

        Assert.True(configuration >= 0 && categories > configuration, "README.md must have ## Configuration before ## Categories file.");
        Assert.Single(lines, line => line == Paragraph);
        Assert.True(paragraph > configuration && paragraph < categories, "The CraftFromBase paragraph must be inside ## Configuration.");
        Assert.True(debugLogging > configuration, "The DebugLogging paragraph must be inside ## Configuration.");
        Assert.Equal(debugLogging + 2, paragraph);
    }
}
