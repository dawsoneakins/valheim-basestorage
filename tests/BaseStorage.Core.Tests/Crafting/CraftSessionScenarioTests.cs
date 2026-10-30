namespace BaseStorage.Core.Tests.Crafting;

using BaseStorage.Core.Abstractions;
using BaseStorage.Core.Availability;
using BaseStorage.Core.Snapshots;
using BaseStorage.Core.Ui;
using BaseStorage.Plugin.Crafting;

/// <summary>GW-U-1110 to GW-U-1113, GW-U-1117 and GW-U-1118 (AC-1, AC-2, AC-3; contract 5.6 S1, S6, S12, S13 and CF7).</summary>
public sealed class CraftSessionScenarioTests
{
    // GW-U-1110 (S1 network grant): the split craft is granted, committed and ends Created with the planned stacks gone.
    [Fact]
    public void ANetworkGrantCommitsAndEndsCreated()
    {
        var rig = new CraftRig();
        ConsumptionPlan plan = rig.Plan() ?? throw new InvalidOperationException("The fixture has no plan.");

        Assert.False(rig.Session.OnDoCrafting(rig.Selection, plan));
        rig.Session.Tick();
        Assert.Equal(CraftPhase.Executing, rig.Session.Phase);

        rig.Grant(rig.Chest7);
        rig.Driver.Tick();
        rig.Session.Tick();

        Assert.Equal(CraftPhase.Idle, rig.Session.Phase);
        Assert.Equal(CraftEnd.Created, rig.Session.LastEnd);
        Assert.Equal(new[] { "ForgetTaken", "Commit", "ForgetTaken" }, rig.CraftGame.Calls.Where(call => call is "ForgetTaken" or "Commit").ToArray());
        // Edited by S-E008-001 (ADR-GWIN-153, FA1): a created craft shows the success line once, not silence.
        Assert.Equal(new[] { StringKeys.MessageCraftUsedBase }, rig.CraftGame.Notices);
        Assert.Empty(rig.World.ContentsOf(CraftFixtures.Inventory));
        Assert.Equal(new[] { "Resin 2" }, rig.Describe(CraftFixtures.C5));
        Assert.Equal(new[] { "Wood 6" }, rig.Describe(CraftFixtures.C7));
        Assert.All(new[] { CraftFixtures.C5, CraftFixtures.C7 }, id => Assert.Equal(ReservationStatus.None, rig.StatusOf(id)));
        Assert.Equal(1, rig.Session.Token);
    }

    // GW-U-1111 (S12): during the commit callback CommitAnswer is true and CountsChests is false.
    [Fact]
    public void DuringTheCommitTheCommitAnswerIsTrueAndTheChestsAreNotCounted()
    {
        var rig = new CraftRig();
        bool? answer = null;
        bool? counts = null;

        rig.RunCommit(_ =>
        {
            answer = rig.Session.CommitAnswer(rig.Recipe);
            counts = rig.Session.CountsChests(rig.Recipe);
            rig.Session.OnConsumeResources(true);
        });

        Assert.Equal(true, answer);
        Assert.Equal(false, counts);
    }

    // GW-U-1111 (S12): after OnConsumeResources(true) ItemCreated is true and CommitAnswer is false.
    [Fact]
    public void AfterTheCommittedResourcesItemCreatedIsTrueAndTheAnswerIsFalse()
    {
        var rig = new CraftRig();
        bool? created = null;
        bool? answerAfter = null;

        rig.RunCommit(_ =>
        {
            rig.Session.OnConsumeResources(true);
            created = rig.Session.ItemCreated;
            answerAfter = rig.Session.CommitAnswer(rig.Recipe);
        });

        Assert.Equal(true, created);
        Assert.Equal(false, answerAfter);
    }

    // GW-U-1111: a different recipe object never gets the commit answer.
    [Fact]
    public void ADifferentRecipeNeverGetsTheCommitAnswer()
    {
        var rig = new CraftRig();
        bool? other = null;

        rig.RunCommit(_ =>
        {
            other = rig.Session.CommitAnswer(new object());
            rig.Session.OnConsumeResources(true);
        });

        Assert.Equal(false, other);
    }

    // GW-U-1112 (S1 plan shape): inventory Wood 3, c5 Wood 8, c7 Wood 4, c5 Resin 2 with ExpectedContainers c5 and c7.
    [Fact]
    public void TheSplitPlanTakesTheInventoryFirstThenTheNearestChest()
    {
        var rig = new CraftRig();
        ConsumptionPlan plan = rig.Plan() ?? throw new InvalidOperationException("The fixture has no plan.");

        Assert.Equal(
            new[] { (CraftFixtures.Inventory, "Wood", 3), (CraftFixtures.C5, "Wood", 8), (CraftFixtures.C7, "Wood", 4), (CraftFixtures.C5, "Resin", 2) },
            plan.Takes.Select(take => (take.ContainerId, take.Stack.PrefabName, take.Count)).ToArray());
        // ExpectedContainers is ascending by ContainerId (UserId first), so Erik's c7 precedes c5 (edited by S-E003-004, ADR-GWIN-124).
        Assert.Equal(new[] { CraftFixtures.C7, CraftFixtures.C5 }, plan.ExpectedContainers.Select(container => container.Id).ToArray());
    }

    // GW-U-1112: FindFirstMissing is null with c7 as a member, and Wood once c7 is removed from the members.
    [Fact]
    public void TheFirstMissingLookupCountsTheChestsOfTheBase()
    {
        var rig = new CraftRig();
        ContainerSnapshot inventory = rig.World.Read(CraftFixtures.Inventory)!;
        ContainerSnapshot c5 = rig.World.Read(CraftFixtures.C5)!;
        ContainerSnapshot c7 = rig.World.Read(CraftFixtures.C7)!;

        Assert.Null(BaseAvailability.FindFirstMissing(CraftFixtures.Snapshot(inventory, new[] { c5, c7 }), CraftFixtures.Requirements()));

        RequirementAvailability? missing = BaseAvailability.FindFirstMissing(CraftFixtures.Snapshot(inventory, new[] { c5 }), CraftFixtures.Requirements());
        Assert.NotNull(missing);
        Assert.Equal("Wood", missing!.Requirement.PrefabName);
    }

    // GW-U-1113 (S6, CF7): with inventory Wood 20 and Resin 5 the plan has no chest take, so vanilla consumes.
    [Fact]
    public void AnInventoryOnlyCraftLetsVanillaConsumeAndWritesNothing()
    {
        var rig = new CraftRig();
        rig.World.SetInventory(CraftFixtures.Wood(20), CraftFixtures.Resin(5));
        ConsumptionPlan plan = rig.Plan() ?? throw new InvalidOperationException("The fixture has no plan.");
        Assert.Empty(plan.ExpectedContainers);

        Assert.True(rig.Session.OnDoCrafting(rig.Selection, plan));

        Assert.Equal(CraftPhase.Idle, rig.Session.Phase);
        Assert.True(rig.Session.VanillaCraftRunning);
        Assert.Empty(rig.Reservations.Calls);
        Assert.Empty(rig.CraftGame.Notices);
        Assert.Empty(rig.Sink.Entries);

        rig.Session.Tick();

        Assert.False(rig.Session.VanillaCraftRunning);
    }

    // GW-U-1117 (S13): building consumption passes while idle, and ItemCreated stays false.
    [Fact]
    public void BuildingConsumptionPassesWhileIdle()
    {
        var rig = new CraftRig();

        Assert.True(rig.Session.OnConsumeResources(false));
        Assert.False(rig.Session.ItemCreated);
        Assert.Empty(rig.CraftGame.Calls);
    }

    // GW-U-1117 (S13): building consumption passes during a commit, and ItemCreated stays false until the committed call.
    [Fact]
    public void BuildingConsumptionPassesDuringACommit()
    {
        var rig = new CraftRig();
        bool? building = null;
        bool? createdAfterBuilding = null;

        rig.RunCommit(_ =>
        {
            building = rig.Session.OnConsumeResources(false);
            createdAfterBuilding = rig.Session.ItemCreated;
            rig.Session.OnConsumeResources(true);
        });

        Assert.Equal(true, building);
        Assert.Equal(false, createdAfterBuilding);
    }

    // GW-U-1118 (CF5 and the gate closed): with the gate closed, the following OnConsumeResources(true) returns true.
    [Fact]
    public void AGateClosedCraftLeavesTheConsumeToVanilla()
    {
        var rig = new CraftRig();

        Assert.True(rig.Session.OnDoCrafting(null, null));

        Assert.True(rig.Session.OnConsumeResources(true));
    }

    // GW-U-1118 (CF7): with an inventory-only plan, the following OnConsumeResources(true) returns true.
    [Fact]
    public void AnInventoryOnlyCraftLeavesTheConsumeToVanilla()
    {
        var rig = new CraftRig();
        rig.World.SetInventory(CraftFixtures.Wood(20), CraftFixtures.Resin(5));
        ConsumptionPlan plan = rig.Plan() ?? throw new InvalidOperationException("The fixture has no plan.");

        Assert.True(rig.Session.OnDoCrafting(rig.Selection, plan));

        Assert.True(rig.Session.OnConsumeResources(true));
    }

    // GW-U-1814 (AC-1, T6, section 5 O3): a craft already executing when CraftFromBase goes off stays in flight. OnDoCrafting(null, null)
    // answers vanilla without touching the session, and the next Tick commits and ends Created with the planned stacks taken.
    [Fact]
    public void AnInFlightCraftSurvivesASettingOffCallAndCommitsOnTheNextTick()
    {
        var rig = new CraftRig();
        ConsumptionPlan plan = rig.Plan() ?? throw new InvalidOperationException("The fixture has no plan.");
        Assert.False(rig.Session.OnDoCrafting(rig.Selection, plan));
        rig.Session.Tick();
        Assert.Equal(CraftPhase.Executing, rig.Session.Phase);
        var selection = rig.Session.Selection;
        var execution = rig.Session.Execution;

        Assert.True(rig.Session.OnDoCrafting(null, null));

        Assert.Equal(CraftPhase.Executing, rig.Session.Phase);
        Assert.Equal(selection, rig.Session.Selection);
        Assert.Equal(execution, rig.Session.Execution);
        Assert.Null(rig.Session.LastEnd);

        rig.Grant(rig.Chest7);
        rig.Driver.Tick();
        rig.Session.Tick();

        Assert.Equal(CraftPhase.Idle, rig.Session.Phase);
        Assert.Equal(CraftEnd.Created, rig.Session.LastEnd);
        // Edited by S-E008-001 (ADR-GWIN-153, FA1): the in-flight craft that commits ends Created and shows the success line once.
        Assert.Equal(new[] { StringKeys.MessageCraftUsedBase }, rig.CraftGame.Notices);
        Assert.Equal(new[] { "Resin 2" }, rig.Describe(CraftFixtures.C5));
        Assert.Equal(new[] { "Wood 6" }, rig.Describe(CraftFixtures.C7));
    }
}
