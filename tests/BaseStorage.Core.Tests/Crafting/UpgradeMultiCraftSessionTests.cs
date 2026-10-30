namespace BaseStorage.Core.Tests.Crafting;

using System;
using System.Collections.Generic;
using System.Linq;
using BaseStorage.Core.Abstractions;
using BaseStorage.Core.Availability;
using BaseStorage.Core.Snapshots;
using BaseStorage.Core.Tests.Fakes;
using BaseStorage.Core.Ui;
using BaseStorage.Plugin.Crafting;
using Xunit;

/// <summary>
/// GW-U-1709 to GW-U-1719 (AC-1 to AC-4, AC-6; contract upgrade-multicraft 4.4 US1-US6 and MS1-MS5): upgrade and multi-craft sessions
/// through CraftSession over CraftRig. Every expected value is a literal of the contract table.
/// </summary>
public sealed class UpgradeMultiCraftSessionTests
{
    private const string StartedLine = "Craft 1: started, 5 take(s) from 2 chest(s)";

    // GW-U-1709 (AC-1, US1 upgrade created): the upgrade is committed once with the upgrade item and quality 4, and the totals follow the plan.
    [Fact]
    public void AnUpgradeIsCreatedAndCommittedWithTheUpgradeItemAtLevelFour()
    {
        var rig = Seed(UpgradeMultiCraftFixtures.WorldU(7, 4));
        object upgradeItem = new object();
        var selection = new CraftSelection(rig.Recipe, upgradeItem, rig.Station, 4, 1);
        List<CraftSelection> committed = Commits(rig);

        Run(rig, selection, PlanOf(rig, UpgradeMultiCraftFixtures.SwordLines(4), 1));

        Assert.Equal(CraftEnd.Created, rig.Session.LastEnd);
        Assert.Single(committed);
        Assert.True(ReferenceEquals(upgradeItem, committed[0].UpgradeItem));
        Assert.Equal(4, committed[0].QualityLevel);
        // Edited by S-E008-001 (ADR-GWIN-153, FA1): the created upgrade shows the success line once.
        Assert.Equal(new[] { StringKeys.MessageCraftUsedBase }, rig.CraftGame.Notices);
        Assert.Contains(StartedLine, rig.Sink.Entries.Select(entry => entry.Message));
        Assert.Equal(0, rig.World.Total("Bronze"));
        Assert.Equal(6, rig.World.Total("Wood"));
        Assert.Equal(6, rig.World.Total("LeatherScraps"));
        Assert.Equal(1, rig.World.Total("SwordBronze"));
    }

    // GW-U-1709 (AC-2, US1 payment order): the upgrade is paid through the executor before Commit, inventory first, then c5, then c7.
    [Fact]
    public void TheUpgradeIsPaidInventoryFirstThenChestFiveThenChestSevenBeforeTheCommit()
    {
        var rig = Seed(UpgradeMultiCraftFixtures.WorldU(7, 4));
        var selection = new CraftSelection(rig.Recipe, new object(), rig.Station, 4, 1);
        var takesSeenAtCommit = new List<int>();
        rig.CraftGame.CommitBody = _ =>
        {
            takesSeenAtCommit.Add(rig.World.TakeCount);
            rig.Session.OnConsumeResources(true);
        };

        Run(rig, selection, PlanOf(rig, UpgradeMultiCraftFixtures.SwordLines(4), 1));

        // The take record is "Take <container> <stack index> <count>"; the stack index is the one current at the take, which shifts once
        // the Bronze stack of the inventory is emptied, so the order is asserted as container and count (inventory 5, c5 7, c7 4, inventory 4 and 4).
        string[] takes = rig.World.Calls
            .Where(call => call.StartsWith("Take", StringComparison.Ordinal))
            .Select(call => call.Split(' '))
            .Select(parts => parts[1] + ":" + parts[3])
            .ToArray();
        Assert.Equal(new[] { "0:5", "5:7", "7:4", "0:4", "0:4" }, takes);
        Assert.Equal(new[] { 5 }, takesSeenAtCommit);
    }

    // GW-U-1710 (AC-2, US2 upgrade denied): when chest c7 answers no, nothing is consumed, the item is not upgraded and one notice is shown.
    [Fact]
    public void ADeniedUpgradeConsumesNothingAndShowsOneNotice()
    {
        var rig = Seed(UpgradeMultiCraftFixtures.WorldU(7, 4));
        var selection = new CraftSelection(rig.Recipe, new object(), rig.Station, 4, 1);
        IReadOnlyList<ContainerSnapshot> before = rig.World.Views().ToArray();

        Assert.False(rig.Session.OnDoCrafting(selection, PlanOf(rig, UpgradeMultiCraftFixtures.SwordLines(4), 1)));
        rig.Session.Tick();
        rig.Driver.OnOpenResponse(rig.Chest7, false);
        rig.Driver.Tick();
        rig.Session.Tick();

        Assert.Equal(CraftEnd.Failed, rig.Session.LastEnd);
        Assert.Equal(new[] { StringKeys.MessageCraftMaterialsMoved }, rig.CraftGame.Notices);
        Assert.DoesNotContain("Commit", rig.CraftGame.Calls);
        Assert.Equal(16, rig.World.Total("Bronze"));
        Assert.Equal(10, rig.World.Total("Wood"));
        Assert.Equal(10, rig.World.Total("LeatherScraps"));
        Assert.Equal(1, rig.World.Total("SwordBronze"));
        ConservationAssert.Holds(before, rig.World.Views());
    }

    // GW-U-1711 (AC-2, US3 upgrade contents changed): a stack changed in chest c5 before the take fails the craft, with c5 named, and nothing is committed.
    [Fact]
    public void AChangedChestFiveFailsTheUpgradeWithNothingCommitted()
    {
        var rig = Seed(UpgradeMultiCraftFixtures.WorldU(7, 4));
        var selection = new CraftSelection(rig.Recipe, new object(), rig.Station, 4, 1);
        List<CraftSelection> committed = Commits(rig);

        Assert.False(rig.Session.OnDoCrafting(selection, PlanOf(rig, UpgradeMultiCraftFixtures.SwordLines(4), 1)));
        rig.World.MoveOut(CraftFixtures.C5, 0, 1);
        rig.Session.Tick();
        rig.Grant(rig.Chest7);
        rig.Driver.Tick();
        rig.Session.Tick();

        Assert.Equal(CraftEnd.Failed, rig.Session.LastEnd);
        Assert.Contains("Craft 1: Failed (ContentsChanged at 3141592653:5)", rig.Sink.Entries.Select(entry => entry.Message));
        Assert.Equal(new[] { StringKeys.MessageCraftMaterialsMoved }, rig.CraftGame.Notices);
        Assert.Empty(committed);
        Assert.Equal(15, rig.World.Total("Bronze"));
        Assert.Equal(10, rig.World.Total("Wood"));
        Assert.Equal(10, rig.World.Total("LeatherScraps"));
    }

    // GW-U-1712 (AC-2, US4 pre-flight fails, PF3): when the upgrade item is gone at pre-flight, no reservation is sent and nothing is consumed.
    [Fact]
    public void AFailedPreFlightForAnUpgradeSendsNothingAndConsumesNothing()
    {
        var rig = Seed(UpgradeMultiCraftFixtures.WorldU(7, 4));
        var selection = new CraftSelection(rig.Recipe, new object(), rig.Station, 4, 1);
        rig.CraftGame.PreFlightResult = false;

        Assert.False(rig.Session.OnDoCrafting(selection, PlanOf(rig, UpgradeMultiCraftFixtures.SwordLines(4), 1)));

        Assert.Equal(CraftEnd.PreFlightFailed, rig.Session.LastEnd);
        Assert.Equal(new[] { StringKeys.MessageCraftMaterialsMoved }, rig.CraftGame.Notices);
        Assert.DoesNotContain(rig.Reservations.Calls, call => call.StartsWith("SendOpenRequest", StringComparison.Ordinal));
        Assert.Equal(16, rig.World.Total("Bronze"));
        Assert.Equal(10, rig.World.Total("Wood"));
        Assert.Equal(10, rig.World.Total("LeatherScraps"));
    }

    // GW-U-1713 (AC-6, US5 upgrade one less): with one Bronze less than the level 4 need there is no plan, the craft is unavailable and nothing changes.
    [Fact]
    public void AnUpgradeOneBronzeShortIsUnavailableAndChangesNothing()
    {
        var rig = Seed(UpgradeMultiCraftFixtures.WorldU(7, 3));
        var selection = new CraftSelection(rig.Recipe, new object(), rig.Station, 4, 1);
        List<CraftSelection> committed = Commits(rig);

        Assert.False(rig.Session.OnDoCrafting(selection, null));

        Assert.Equal(CraftEnd.Unavailable, rig.Session.LastEnd);
        Assert.Equal(new[] { StringKeys.MessageCraftMaterialsMoved }, rig.CraftGame.Notices);
        Assert.Equal(1, rig.Session.Token);
        Assert.Empty(committed);
        Assert.Equal(15, rig.World.Total("Bronze"));
        Assert.Equal(10, rig.World.Total("Wood"));
        Assert.Equal(10, rig.World.Total("LeatherScraps"));
    }

    // GW-U-1714 (AC-1, US6 inventory-only upgrade runs vanilla, CF7): with no chest needed, the call returns true and vanilla runs until the next tick.
    [Fact]
    public void AnInventoryOnlyUpgradeRunsVanillaAndChangesNothingInTheMod()
    {
        var rig = Seed(UpgradeMultiCraftFixtures.WorldU(7, 4));
        var selection = new CraftSelection(rig.Recipe, new object(), rig.Station, 2, 1);

        Assert.True(rig.Session.OnDoCrafting(selection, PlanOf(rig, UpgradeMultiCraftFixtures.SwordLines(2), 1)));

        Assert.Equal(CraftPhase.Idle, rig.Session.Phase);
        Assert.True(rig.Session.VanillaCraftRunning);
        rig.Session.Tick();
        Assert.False(rig.Session.VanillaCraftRunning);
        Assert.Equal(16, rig.World.Total("Bronze"));
        Assert.Equal(10, rig.World.Total("Wood"));
        Assert.Equal(10, rig.World.Total("LeatherScraps"));
    }

    // GW-U-1715 (AC-4, MS1 multi-craft x5 created): five crafts of arrows take 40 Wood and 10 Feathers, inventory first, and the item commits once with multiplier 5.
    [Fact]
    public void AMultiCraftOfFiveIsCreatedAndConsumesTheMultipliedAmounts()
    {
        var rig = Seed(UpgradeMultiCraftFixtures.WorldA());
        var selection = new CraftSelection(rig.Recipe, null, rig.Station, 1, 5);
        List<CraftSelection> committed = Commits(rig);

        Run(rig, selection, PlanOf(rig, UpgradeMultiCraftFixtures.ArrowLines(), 5));

        Assert.Equal(CraftEnd.Created, rig.Session.LastEnd);
        Assert.Single(committed);
        Assert.Equal(5, committed[0].Multiplier);
        // Edited by S-E008-001 (ADR-GWIN-153, FA1): the created multi-craft shows the success line once.
        Assert.Equal(new[] { StringKeys.MessageCraftUsedBase }, rig.CraftGame.Notices);
        Assert.Contains("Craft 1: started, 6 take(s) from 2 chest(s)", rig.Sink.Entries.Select(entry => entry.Message));
        Assert.Equal(8, rig.World.Total("Wood"));
        Assert.Equal(2, rig.World.Total("Feathers"));
        Assert.Empty(rig.World.ContentsOf(CraftFixtures.Inventory));
        Assert.Empty(rig.World.ContentsOf(CraftFixtures.C5));
        Assert.Equal(new[] { "Wood 8", "Feathers 2" }, rig.Describe(CraftFixtures.C7));
    }

    // GW-U-1716 (AC-4, MS2 multi-craft x6 exact): six crafts take exactly the 48 Wood and 12 Feathers there are.
    [Fact]
    public void ASixCraftMultiCraftOfTheExactAmountIsCreatedAndLeavesNothing()
    {
        var rig = Seed(UpgradeMultiCraftFixtures.WorldA());
        var selection = new CraftSelection(rig.Recipe, null, rig.Station, 1, 6);

        Run(rig, selection, PlanOf(rig, UpgradeMultiCraftFixtures.ArrowLines(), 6));

        Assert.Equal(CraftEnd.Created, rig.Session.LastEnd);
        // Edited by S-E008-001 (ADR-GWIN-153, FA1): the six-craft multi-craft that ends Created shows the success line once.
        Assert.Equal(new[] { StringKeys.MessageCraftUsedBase }, rig.CraftGame.Notices);
        Assert.Equal(0, rig.World.Total("Wood"));
        Assert.Equal(0, rig.World.Total("Feathers"));
    }

    // GW-U-1717 (AC-4, MS3 multi-craft denied): when chest c7 answers no, the multi-craft is not made and every Wood and Feather stays.
    [Fact]
    public void ADeniedMultiCraftConsumesNothing()
    {
        var rig = Seed(UpgradeMultiCraftFixtures.WorldA());
        var selection = new CraftSelection(rig.Recipe, null, rig.Station, 1, 5);
        List<CraftSelection> committed = Commits(rig);

        Assert.False(rig.Session.OnDoCrafting(selection, PlanOf(rig, UpgradeMultiCraftFixtures.ArrowLines(), 5)));
        rig.Session.Tick();
        rig.Driver.OnOpenResponse(rig.Chest7, false);
        rig.Driver.Tick();
        rig.Session.Tick();

        Assert.Equal(CraftEnd.Failed, rig.Session.LastEnd);
        Assert.Equal(new[] { StringKeys.MessageCraftMaterialsMoved }, rig.CraftGame.Notices);
        Assert.Empty(committed);
        Assert.Equal(48, rig.World.Total("Wood"));
        Assert.Equal(12, rig.World.Total("Feathers"));
    }

    // GW-U-1718 (AC-4, MS4 multi-craft abandoned, A3): when the selection stops matching while executing, the craft is abandoned silently and chest c5 is released.
    [Fact]
    public void AnAbandonedMultiCraftIsSilentReleasesChestFiveAndConsumesNothing()
    {
        var rig = Seed(UpgradeMultiCraftFixtures.WorldA());
        var selection = new CraftSelection(rig.Recipe, null, rig.Station, 1, 5);
        List<CraftSelection> committed = Commits(rig);

        Assert.False(rig.Session.OnDoCrafting(selection, PlanOf(rig, UpgradeMultiCraftFixtures.ArrowLines(), 5)));
        Assert.Equal(CraftPhase.Executing, rig.Session.Phase);
        rig.CraftGame.StillSelected = false;
        rig.Session.Tick();

        Assert.Equal(CraftEnd.Abandoned, rig.Session.LastEnd);
        Assert.Empty(rig.CraftGame.Notices);
        Assert.Empty(committed);
        Assert.Equal(ReservationStatus.None, rig.StatusOf(CraftFixtures.C5));
        Assert.Equal(48, rig.World.Total("Wood"));
        Assert.Equal(12, rig.World.Total("Feathers"));
    }

    // GW-U-1719 (AC-3, MS5 multi-craft x7 short): seven crafts need 56 Wood, so the craft is unavailable and nothing changes.
    [Fact]
    public void ASevenCraftMultiCraftIsUnavailableAndChangesNothing()
    {
        var rig = Seed(UpgradeMultiCraftFixtures.WorldA());
        var selection = new CraftSelection(rig.Recipe, null, rig.Station, 1, 7);
        List<CraftSelection> committed = Commits(rig);

        Assert.False(rig.Session.OnDoCrafting(selection, null));

        Assert.Equal(CraftEnd.Unavailable, rig.Session.LastEnd);
        Assert.Equal(new[] { StringKeys.MessageCraftMaterialsMoved }, rig.CraftGame.Notices);
        Assert.Empty(committed);
        Assert.Equal(48, rig.World.Total("Wood"));
        Assert.Equal(12, rig.World.Total("Feathers"));
    }

    private static CraftRig Seed(UpgradeMultiCraftFixtures.StackWorld world)
    {
        var rig = new CraftRig();
        rig.World.SetInventory(world.Inventory);
        rig.World.AddChest(rig.Chest5, world.C5);
        rig.World.AddChest(rig.Chest7, world.C7);
        return rig;
    }

    private static ConsumptionPlan PlanOf(CraftRig rig, RecipeLine[] lines, int multiplier)
    {
        var snapshot = CraftFixtures.Snapshot(
            rig.World.Read(CraftFixtures.Inventory)!,
            new[] { rig.World.Read(CraftFixtures.C5)!, rig.World.Read(CraftFixtures.C7)! });
        return ConsumptionPlanner.Plan(snapshot, UpgradeMultiCraftFixtures.Requirements(lines, multiplier))
            ?? throw new InvalidOperationException("The fixture has no plan.");
    }

    private static List<CraftSelection> Commits(CraftRig rig)
    {
        var committed = new List<CraftSelection>();
        rig.CraftGame.CommitBody = selection =>
        {
            committed.Add(selection);
            rig.Session.OnConsumeResources(true);
        };
        return committed;
    }

    private static void Run(CraftRig rig, CraftSelection selection, ConsumptionPlan plan)
    {
        Assert.False(rig.Session.OnDoCrafting(selection, plan));
        rig.Session.Tick();
        rig.Grant(rig.Chest7);
        rig.Driver.Tick();
        rig.Session.Tick();
    }
}
