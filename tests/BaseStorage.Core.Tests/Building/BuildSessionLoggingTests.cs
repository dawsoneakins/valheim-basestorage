namespace BaseStorage.Core.Tests.Building;

using BaseStorage.Core.Abstractions;
using BaseStorage.Plugin.Building;

/// <summary>GW-U-1623 (AC-6, contract 5.4 constructor and section 6 BK1, BK2): the constructor checks and the Debug lines of a build.</summary>
public sealed class BuildSessionLoggingTests
{
    // GW-U-1623 (6, T1): the start and end lines are interleaved with the driver's reservation lines.
    [Fact]
    public void T1WritesTheStartAndEndLinesInterleavedWithTheDriverLines()
    {
        var rig = new BuildRig();

        rig.RunS1();

        string[] lines = rig.Sink.Entries.Select(entry => entry.Message).ToArray();
        AssertInOrder(
            lines,
            "Reserve 2718281828:7 [craft]: route SendRequest",
            "Reserve 3141592653:5 [craft]: route LocalOwner",
            "Build 1: started, 4 take(s) from 2 chest(s)",
            "Reserve 2718281828:7 [craft]: open response granted consumed, chest UI not shown",
            "Build 1: Placed");
    }

    // GW-U-1623 (6): with Debug disabled no line is written and the build still completes.
    [Fact]
    public void WithDebugDisabledNoLineIsWrittenAndTheBuildCompletes()
    {
        var rig = new BuildRig();
        rig.Sink.Enabled = severity => severity != LogSeverity.Debug;

        rig.RunS1();

        Assert.Empty(rig.Sink.Entries);
        Assert.Equal(BuildEnd.Placed, rig.Session.LastEnd);
    }

    // GW-U-1623 (5.4 constructor): null game and executor throw by ParamName, a null sink is allowed, and the constructor reads nothing.
    [Fact]
    public void TheConstructorChecksItsArgumentsAndReadsNothing()
    {
        var rig = new BuildRig();

        ArgumentNullException noGame = Assert.Throws<ArgumentNullException>(() => new BuildSession(null!, rig.Executor, rig.Sink));
        Assert.Equal("game", noGame.ParamName);
        ArgumentNullException noExecutor = Assert.Throws<ArgumentNullException>(() => new BuildSession(rig.BuildGame, null!, rig.Sink));
        Assert.Equal("executor", noExecutor.ParamName);

        var quiet = new BuildSession(rig.BuildGame, rig.Executor, null);

        Assert.Equal(0, quiet.Token);
        Assert.Empty(rig.BuildGame.Calls);
        Assert.Empty(rig.Reservations.Calls);
        Assert.Empty(rig.Sink.Entries);
    }

    private static void AssertInOrder(string[] lines, params string[] expected)
    {
        int last = -1;
        foreach (string line in expected)
        {
            int index = Array.FindIndex(lines, candidate => candidate == line);
            Assert.True(index > last, $"Expected '{line}' after position {last} in: {string.Join(" | ", lines)}");
            last = index;
        }
    }
}
