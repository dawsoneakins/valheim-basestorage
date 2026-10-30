namespace BaseStorage.Core.Tests.Crafting;

using BaseStorage.Core.Abstractions;
using BaseStorage.Plugin.Crafting;

/// <summary>GW-U-1116 (AC-2, contract 6): the constructor checks and the Debug lines of a craft.</summary>
public sealed class CraftSessionLoggingTests
{
    // GW-U-1116 (6, S1): the start and end lines are interleaved with the driver's reservation lines.
    [Fact]
    public void S1WritesTheStartAndEndLinesInterleavedWithTheDriverLines()
    {
        var rig = new CraftRig();

        rig.RunS1();

        string[] lines = rig.Sink.Entries.Select(entry => entry.Message).ToArray();
        AssertInOrder(
            lines,
            // Edited by S-E003-004, ADR-GWIN-124: Execute requests the chests in ascending id order (c7 before c5) and the
            // route lines come during Execute, before K1 (the OnDoCrafting step order runs K1 after Execute).
            "Reserve 2718281828:7 [craft]: route SendRequest",
            "Reserve 3141592653:5 [craft]: route LocalOwner",
            "Craft 1: started, 4 take(s) from 2 chest(s)",
            "Reserve 2718281828:7 [craft]: open response granted consumed, chest UI not shown",
            "Craft 1: Created");
    }

    // GW-U-1116: with Debug disabled no line is written and the craft still completes.
    [Fact]
    public void WithDebugDisabledNoLineIsWritten()
    {
        var rig = new CraftRig();
        rig.Sink.Enabled = severity => severity != LogSeverity.Debug;

        rig.RunS1();

        Assert.Empty(rig.Sink.Entries);
        Assert.Equal(CraftEnd.Created, rig.Session.LastEnd);
    }

    // GW-U-1116: null game and null executor throw by name.
    [Fact]
    public void NullGameOrExecutorThrowByName()
    {
        var rig = new CraftRig();

        ArgumentNullException game = Assert.Throws<ArgumentNullException>(() => new CraftSession(null!, rig.Executor, rig.Sink));
        Assert.Equal("game", game.ParamName);
        ArgumentNullException executor = Assert.Throws<ArgumentNullException>(() => new CraftSession(rig.CraftGame, null!, rig.Sink));
        Assert.Equal("executor", executor.ParamName);
    }

    // GW-U-1116: a null sink is allowed, and the constructor reads nothing.
    [Fact]
    public void ANullSinkIsAllowedAndTheConstructorReadsNothing()
    {
        var rig = new CraftRig();

        var quiet = new CraftSession(rig.CraftGame, rig.Executor, null);

        Assert.Equal(CraftPhase.Idle, quiet.Phase);
        Assert.Empty(rig.CraftGame.Calls);
        Assert.Empty(rig.Sink.Entries);
    }

    private static void AssertInOrder(IReadOnlyList<string> lines, params string[] expected)
    {
        int cursor = 0;
        foreach (string line in expected)
        {
            int found = -1;
            for (int index = cursor; index < lines.Count; index++)
            {
                if (lines[index] == line)
                {
                    found = index;
                    break;
                }
            }

            Assert.True(found >= 0, "Missing, or out of order: " + line);
            cursor = found + 1;
        }
    }
}
