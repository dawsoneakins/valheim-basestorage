namespace BaseStorage.Core.Tests.Requirements;

using System;
using System.Linq;
using BaseStorage.Core.Ui;
using BaseStorage.Plugin.Requirements;

/// <summary>
/// GW-U-1515 to GW-U-1521 (AC-2 to AC-5; contract 4.2 and 4.3, rows R1-R18): the game-free display session over fixture D and
/// <see cref="FakeRequirementDisplayWorld"/>. Expected values are literals of the contract tables. No Plugin patch or game type is used.
/// </summary>
public sealed class RequirementDisplaySessionTests
{
    private static (RequirementDisplaySession Session, FakeRequirementDisplayWorld World) Panel(int quality = 1, int multiplier = 1)
    {
        var world = new FakeRequirementDisplayWorld();
        var session = new RequirementDisplaySession(world);
        session.EnterCraftingPanel(quality, multiplier);
        return (session, world);
    }

    // GW-U-1515 (AC-4, R1-R5): the session renders through the formatter over fixture D; c7 Wood (open by another player) is never counted.
    [Theory]
    [InlineData("Wood", 20, "20", "52/20", false)]
    [InlineData("Wood", 60, "60", "52/60", true)]
    [InlineData("Wood", 52, "52", "52/52", false)]
    [InlineData("Resin", 5, "5", "5/5", false)]
    [InlineData("Resin", 2, "2", "5/2", false)]
    public void ThePanelRendersTheCombinedFormAndReadsOnce(string prefab, long need, string vanilla, string expectedText, bool expectedShort)
    {
        var (session, world) = Panel();

        RequirementDisplay? display = session.Resolve(prefab, need, vanilla);

        Assert.True(display.HasValue);
        Assert.Equal(expectedText, display!.Value.Text);
        Assert.Equal(expectedShort, display.Value.IsShort);
        Assert.True(display.Value.ShowsChests);
        Assert.Equal(new[] { "ReadCraftingPanel(1, 1)" }, world.Calls.ToArray());
    }

    // GW-U-1516 (AC-2, R6): a prefab whose only stock is in an open chest keeps the vanilla text, the same instance, and is short.
    [Fact]
    public void AnOpenChestOnlyStoneKeepsTheVanillaTextAsTheSameInstance()
    {
        var (session, world) = Panel();
        string vanilla = "4";

        RequirementDisplay? display = session.Resolve("Stone", 4, vanilla);

        Assert.True(display.HasValue);
        Assert.Same(vanilla, display!.Value.Text);
        Assert.True(display.Value.IsShort);
        Assert.False(display.Value.ShowsChests);
        Assert.Single(world.Calls);
    }

    // GW-U-1516 (AC-2, R7): a prefab with no stock anywhere keeps the vanilla text and is short.
    [Fact]
    public void APrefabWithNoStockKeepsTheVanillaText()
    {
        var (session, world) = Panel();

        RequirementDisplay? display = session.Resolve("Flint", 1, "1");

        Assert.True(display.HasValue);
        Assert.Equal("1", display!.Value.Text);
        Assert.True(display.Value.IsShort);
        Assert.False(display.Value.ShowsChests);
        Assert.Single(world.Calls);
    }

    // GW-U-1517 (AC-2, R8): a null, blank or out-of-range request renders vanilla with no further world call.
    [Theory]
    [InlineData(null, 5L, "5")]
    [InlineData("  ", 5L, "5")]
    [InlineData("Wood", 0L, "0")]
    [InlineData("Wood", 2147483648L, "x")]
    [InlineData("Wood", 20L, null)]
    public void AnUnusableRequestResolvesToNullWithoutAWorldCall(string? prefab, long need, string? vanilla)
    {
        var (session, world) = Panel();

        Assert.Null(session.Resolve(prefab, need, vanilla));
        Assert.Single(world.Calls);
    }

    // GW-U-1517 (AC-2, R9): a fresh session resolves to null, has no context, and makes no world call.
    [Fact]
    public void AFreshSessionRendersVanillaAndMakesNoWorldCall()
    {
        var world = new FakeRequirementDisplayWorld();
        var session = new RequirementDisplaySession(world);

        Assert.Null(session.Resolve("Wood", 20, "20"));
        Assert.Equal(RequirementDisplayContext.None, session.Context);
        Assert.Empty(world.Calls);
    }

    // GW-U-1517 (AC-2, R10, R11): a world that returns null leaves the context entered and renders vanilla; a zero quality or multiplier does not read.
    [Fact]
    public void AWorldReturningNullRendersVanillaWithTheContextEntered()
    {
        var world = new FakeRequirementDisplayWorld { ReturnsNull = true };
        var session = new RequirementDisplaySession(world);

        session.EnterCraftingPanel(1, 1);

        Assert.Null(session.Resolve("Wood", 20, "20"));
        Assert.Equal(RequirementDisplayContext.CraftingPanel, session.Context);
        Assert.Equal(new[] { "ReadCraftingPanel(1, 1)" }, world.Calls.ToArray());
    }

    // GW-U-1517 (AC-2, R11): EnterCraftingPanel with a quality or multiplier below 1 makes no world call and leaves no snapshot.
    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 0)]
    public void AnEnterWithAZeroArgumentMakesNoWorldCall(int quality, int multiplier)
    {
        var world = new FakeRequirementDisplayWorld();
        var session = new RequirementDisplaySession(world);

        session.EnterCraftingPanel(quality, multiplier);

        Assert.Equal(RequirementDisplayContext.CraftingPanel, session.Context);
        Assert.Null(session.Snapshot);
        Assert.Null(session.Resolve("Wood", 20, "20"));
        Assert.Empty(world.Calls);
    }

    // GW-U-1518 (AC-5, R16, CD9): a build-tooltip read that throws propagates, and afterwards the session renders vanilla.
    [Fact]
    public void AFailingBuildTooltipReadLeavesVanillaAfterThePropagatedException()
    {
        var world = new FakeRequirementDisplayWorld { ThrowsFromBuildTooltip = true };
        var session = new RequirementDisplaySession(world);

        Assert.Throws<InvalidOperationException>(() => { session.EnterBuildTooltip(); });

        Assert.Equal(RequirementDisplayContext.BuildTooltip, session.Context);
        Assert.Null(session.Snapshot);
        Assert.Null(session.Resolve("Wood", 20, "20"));
        Assert.Equal(new[] { "ReadBuildTooltip()" }, world.Calls.ToArray());
    }

    // GW-U-1518 (AC-5, R15, CD9): a crafting-panel read that throws propagates, and afterwards the session renders vanilla.
    [Fact]
    public void AFailingCraftingPanelReadLeavesVanillaAfterThePropagatedException()
    {
        var world = new FakeRequirementDisplayWorld { ThrowsFromCraftingPanel = true };
        var session = new RequirementDisplaySession(world);

        Assert.Throws<InvalidOperationException>(() => { session.EnterCraftingPanel(1, 1); });

        Assert.Equal(RequirementDisplayContext.CraftingPanel, session.Context);
        Assert.Null(session.Snapshot);
        Assert.Null(session.Resolve("Wood", 20, "20"));
    }

    // GW-U-1518 (AC-5, R15): a leaked CraftingPanel context (no Exit) is overwritten by EnterBuildTooltip, which renders from its own read.
    [Fact]
    public void ALeakedCraftingPanelContextIsOverwrittenByTheBuildTooltip()
    {
        var (session, world) = Panel();

        session.EnterBuildTooltip();

        RequirementDisplay? display = session.Resolve("Wood", 20, "20");
        Assert.True(display.HasValue);
        Assert.Equal("52/20", display!.Value.Text);
        Assert.Equal(RequirementDisplayContext.BuildTooltip, session.Context);
        Assert.Equal(new[] { "ReadCraftingPanel(1, 1)", "ReadBuildTooltip()" }, world.Calls.ToArray());
    }

    // GW-U-1519 (AC-4, R12): EnterCraftingPanel(3, 5) forwards the arguments and stores the snapshot the world returned.
    [Fact]
    public void EnterCraftingPanelForwardsItsArgumentsAndStoresTheSnapshot()
    {
        var (session, world) = Panel(3, 5);

        Assert.Equal(new[] { "ReadCraftingPanel(3, 5)" }, world.Calls.ToArray());
        Assert.Same(world.Snapshot, session.Snapshot);
    }

    // GW-U-1519 (AC-4, R13): Exit with another context, None or an undefined value is a no-op.
    [Fact]
    public void ExitWithAnotherContextNoneOrAnUndefinedValueChangesNothing()
    {
        var (session, world) = Panel();

        session.Exit(RequirementDisplayContext.BuildTooltip);
        Assert.Equal(RequirementDisplayContext.CraftingPanel, session.Context);
        Assert.Same(world.Snapshot, session.Snapshot);

        session.Exit(RequirementDisplayContext.None);
        Assert.Equal(RequirementDisplayContext.CraftingPanel, session.Context);
        Assert.Same(world.Snapshot, session.Snapshot);

        session.Exit((RequirementDisplayContext)99);
        Assert.Equal(RequirementDisplayContext.CraftingPanel, session.Context);
        Assert.Same(world.Snapshot, session.Snapshot);

        Assert.Single(world.Calls);
    }

    // GW-U-1519 (AC-4, R14): Exit of the current context clears the context and the snapshot, and Resolve is then null.
    [Fact]
    public void ExitOfTheCurrentContextClearsItAndResolveIsThenNull()
    {
        var (session, world) = Panel();

        session.Exit(RequirementDisplayContext.CraftingPanel);

        Assert.Equal(RequirementDisplayContext.None, session.Context);
        Assert.Null(session.Snapshot);
        Assert.Null(session.Resolve("Wood", 20, "20"));
        Assert.Single(world.Calls);
    }

    // GW-U-1519 (AC-4, R17): three more Resolve calls give equal results and make no further world call.
    [Fact]
    public void RepeatedResolvesGiveEqualResultsWithoutAnotherWorldCall()
    {
        var (session, world) = Panel();

        RequirementDisplay? first = session.Resolve("Wood", 20, "20");
        RequirementDisplay? second = session.Resolve("Wood", 20, "20");
        RequirementDisplay? third = session.Resolve("Wood", 20, "20");
        RequirementDisplay? fourth = session.Resolve("Wood", 20, "20");

        Assert.Equal(first, second);
        Assert.Equal(first, third);
        Assert.Equal(first, fourth);
        Assert.Single(world.Calls);
    }

    // GW-U-1519 (AC-4, R13, pin): the context enum has exactly None = 0, CraftingPanel = 1 and BuildTooltip = 2, over int.
    [Fact]
    public void TheContextEnumHasExactlyTheContractedValues()
    {
        Assert.Equal(typeof(int), Enum.GetUnderlyingType(typeof(RequirementDisplayContext)));

        string[] names = Enum.GetNames(typeof(RequirementDisplayContext));
        Assert.Equal(3, names.Length);
        Assert.Equal(0, (int)RequirementDisplayContext.None);
        Assert.Equal(1, (int)RequirementDisplayContext.CraftingPanel);
        Assert.Equal(2, (int)RequirementDisplayContext.BuildTooltip);
    }

    // GW-U-1520 (AC-4, R18): a null world is rejected by ParamName.
    [Fact]
    public void ANullWorldIsRejectedByName()
    {
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => { _ = new RequirementDisplaySession(null!); });

        Assert.Equal("world", ex.ParamName);
    }

    // GW-U-1520 (AC-4, R18): the constructor makes no world call and the new session has no context and no snapshot.
    [Fact]
    public void TheConstructorReadsNothingAndStartsWithNoContext()
    {
        var world = new FakeRequirementDisplayWorld();

        var session = new RequirementDisplaySession(world);

        Assert.Empty(world.Calls);
        Assert.Equal(RequirementDisplayContext.None, session.Context);
        Assert.Null(session.Snapshot);
    }

    // GW-U-1521 (AC-3, 3.2, 4.2): the colour bit is the combined rule over fixture D. Wood 13 is not short although the inventory alone (12) is.
    [Theory]
    [InlineData(60, true)]
    [InlineData(52, false)]
    [InlineData(20, false)]
    [InlineData(13, false)]
    public void TheColourBitCountsTheChestAmount(long need, bool expectedShort)
    {
        var (session, _) = Panel();

        RequirementDisplay? display = session.Resolve("Wood", need, need.ToString());

        Assert.True(display.HasValue);
        Assert.Equal(expectedShort, display!.Value.IsShort);
    }
}
