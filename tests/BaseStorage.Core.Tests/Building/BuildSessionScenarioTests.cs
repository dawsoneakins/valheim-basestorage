namespace BaseStorage.Core.Tests.Building;

using BaseStorage.Core.Abstractions;
using BaseStorage.Core.Availability;
using BaseStorage.Core.Tests.Crafting;
using BaseStorage.Core.Tests.Fakes;
using BaseStorage.Core.Ui;
using BaseStorage.Plugin.Building;
using BaseStorage.Plugin.Crafting;

/// <summary>GW-U-1611 to GW-U-1615 (AC-6; contract 5.9 T1, T6, T9, T10, T12, T13): the successful, refused and repeated placements.</summary>
public sealed class BuildSessionScenarioTests
{
    // GW-U-1611 (T1, AC-6): a network grant commits after the take and ends Placed, with the planned stacks gone and no notice.
    [Fact]
    public void ANetworkGrantCommitsAndEndsPlaced()
    {
        var rig = new BuildRig();

        Assert.False(rig.TryPlace(rig.Plan()));
        rig.Session.Tick();
        Assert.Equal(CraftPhase.Executing, rig.Session.Phase);
        Assert.DoesNotContain("Commit", rig.BuildGame.Calls);

        rig.Grant(rig.Chest7);
        rig.Driver.Tick();
        rig.Session.Tick();

        Assert.Equal(CraftPhase.Idle, rig.Session.Phase);
        Assert.Equal(BuildEnd.Placed, rig.Session.LastEnd);
        Assert.Equal(new[] { "ForgetTaken", "Commit", "ForgetTaken" }, rig.BuildGame.Calls.Where(call => call is "ForgetTaken" or "Commit").ToArray());
        // Edited by S-E008-001 (ADR-GWIN-153, FA2): a placed piece shows the success line once, not silence.
        Assert.Equal(new[] { StringKeys.MessageCraftUsedBase }, rig.BuildGame.Notices);
        Assert.Empty(rig.World.ContentsOf(CraftFixtures.Inventory));
        Assert.Equal(new[] { "Resin 2" }, rig.Describe(CraftFixtures.C5));
        Assert.Equal(new[] { "Wood 6" }, rig.Describe(CraftFixtures.C7));
        Assert.All(new[] { CraftFixtures.C5, CraftFixtures.C7 }, id => Assert.Equal(ReservationStatus.None, rig.StatusOf(id)));
        Assert.Equal(1, rig.Session.Token);
        Assert.False(rig.Session.Guarded);
    }

    // GW-U-1612 (T12, AC-6): a commit that does not create the piece returns every taken stack, with no notice.
    [Fact]
    public void ACommitThatDoesNotCreateThePieceReturnsEverything()
    {
        var rig = new BuildRig();
        rig.BuildGame.CommitBody = _ => { };
        var before = rig.World.Views().ToArray();

        rig.RunS1();

        Assert.Equal(CraftPhase.Idle, rig.Session.Phase);
        Assert.Equal(BuildEnd.Returned, rig.Session.LastEnd);
        Assert.Equal(new[] { "Commit", "ReturnTaken" }, rig.BuildGame.Calls.Where(call => call is "Commit" or "ReturnTaken").ToArray());
        Assert.Equal(4, rig.BuildGame.LastReturned);
        Assert.Empty(rig.BuildGame.Notices);
        Assert.Equal(21, rig.World.Total("Wood"));
        Assert.Equal(4, rig.World.Total("Resin"));
        ConservationAssert.Holds(before, rig.World.Views());
    }

    // GW-U-1613 (T13, AC-6): a commit that throws after the piece was created ends Placed, with the exception propagated and the take forgotten.
    [Fact]
    public void ACommitThatThrowsAfterCreationEndsPlacedAndForgetsTheTake()
    {
        var rig = new BuildRig();
        rig.BuildGame.CommitBody = _ =>
        {
            rig.Session.OnPlacePiece(true);
            throw new InvalidOperationException("The commit failed after the piece was created.");
        };
        Assert.False(rig.TryPlace(rig.Plan()));
        rig.Session.Tick();
        rig.Grant(rig.Chest7);
        rig.Driver.Tick();

        Assert.Throws<InvalidOperationException>(() => rig.Session.Tick());

        Assert.Equal(CraftPhase.Idle, rig.Session.Phase);
        Assert.Equal(BuildEnd.Placed, rig.Session.LastEnd);
        // Edited by S-E008-001 (ADR-GWIN-153, FA2): End(Placed) shows the success notice after ForgetTaken, so ForgetTaken is second to last.
        Assert.Equal("ForgetTaken", rig.BuildGame.Calls[^2]);
        Assert.Equal("ShowNotice", rig.BuildGame.Calls[^1]);
        // Edited by S-E008-001 (ADR-GWIN-153, FA2): a commit that throws after creation still ends Placed and shows the success line once.
        Assert.Equal(new[] { StringKeys.MessageCraftUsedBase }, rig.BuildGame.Notices);
        Assert.Empty(rig.World.ContentsOf(CraftFixtures.Inventory));
        Assert.Equal(new[] { "Resin 2" }, rig.Describe(CraftFixtures.C5));
        Assert.Equal(new[] { "Wood 6" }, rig.Describe(CraftFixtures.C7));
        Assert.Equal(1, rig.Session.Token);
        Assert.False(rig.Session.Guarded);
    }

    // GW-U-1614 (T9, AC-6): a repeated click on the pending piece is refused and changes nothing.
    [Fact]
    public void ARepeatedClickWhilePendingIsRefusedAndChangesNothing()
    {
        var rig = new BuildRig();
        rig.StartFixture();

        Assert.False(rig.TryPlace(rig.Plan()));

        Assert.Equal(CraftPhase.Executing, rig.Session.Phase);
        Assert.Equal(1, rig.Session.Token);
        Assert.False(rig.Session.Guarded);
    }

    // GW-U-1614 (T10, AC-6, CX10): another piece abandons the pending one silently and starts its own execution.
    [Fact]
    public void AnotherPieceAbandonsThePendingOneAndStartsItsOwn()
    {
        var rig = new BuildRig();
        rig.StartFixture();
        var other = new BuildSelection(new object());

        rig.Session.EnterGuard();
        bool run = rig.Session.OnTryPlacePiece(other, rig.Plan());

        Assert.False(run);
        Assert.Equal(CraftPhase.Executing, rig.Session.Phase);
        Assert.Equal(2, rig.Session.Token);
        Assert.Equal(other, rig.Session.Selection);
        Assert.Contains("Build 1: Abandoned", rig.Sink.Entries.Select(entry => entry.Message));
        Assert.Empty(rig.BuildGame.Notices);
    }

    // GW-U-1615 (T6, BF10): inventory-only materials let vanilla run, and nothing is started or reserved.
    [Fact]
    public void InventoryOnlyMaterialsLetVanillaRunWithNothingStarted()
    {
        var rig = new BuildRig();
        rig.World.SetInventory(CraftFixtures.Wood(20), CraftFixtures.Resin(5));
        ConsumptionPlan plan = CraftFixtures.PlanFor(rig.World.Read(CraftFixtures.Inventory)!)
            ?? throw new InvalidOperationException("The inventory alone covers the piece.");

        Assert.True(rig.TryPlace(plan));

        Assert.Equal(CraftPhase.Idle, rig.Session.Phase);
        Assert.False(rig.Session.Guarded);
        Assert.Null(rig.Session.LastEnd);
        Assert.Empty(rig.BuildGame.Calls);
        Assert.Empty(rig.Reservations.Calls);
    }

    // GW-U-1815 (AC-1, T6, section 5 O3): a build already executing when CraftFromBase goes off stays in flight. EnterGuard then
    // OnTryPlacePiece(null, null) answers vanilla without touching the session, and the next Tick commits and ends Placed.
    [Fact]
    public void AnInFlightBuildSurvivesASettingOffCallAndCommitsOnTheNextTick()
    {
        var rig = new BuildRig();
        Assert.False(rig.TryPlace(rig.Plan()));
        rig.Session.Tick();
        Assert.Equal(CraftPhase.Executing, rig.Session.Phase);
        var selection = rig.Session.Selection;

        rig.Session.EnterGuard();
        Assert.True(rig.Session.OnTryPlacePiece(null, null));

        Assert.False(rig.Session.Guarded);
        Assert.Equal(CraftPhase.Executing, rig.Session.Phase);
        Assert.Equal(selection, rig.Session.Selection);
        Assert.Null(rig.Session.LastEnd);

        rig.Grant(rig.Chest7);
        rig.Driver.Tick();
        rig.Session.Tick();

        Assert.Equal(CraftPhase.Idle, rig.Session.Phase);
        Assert.Equal(BuildEnd.Placed, rig.Session.LastEnd);
        // Edited by S-E008-001 (ADR-GWIN-153, FA2): the in-flight build that commits ends Placed and shows the success line once.
        Assert.Equal(new[] { StringKeys.MessageCraftUsedBase }, rig.BuildGame.Notices);
        Assert.Contains("Commit", rig.BuildGame.Calls);
        Assert.Equal(new[] { "Wood 6" }, rig.Describe(CraftFixtures.C7));
    }
}
