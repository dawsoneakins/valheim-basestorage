namespace BaseStorage.Core.Tests.Organize.Running;

using BaseStorage.Core.Bases.Resolution;
using BaseStorage.Core.Organize;
using BaseStorage.Core.Organize.Run;
using BaseStorage.Core.Settings;
using static BaseStorage.Core.Tests.Organize.OrganizeFixtures;
using static BaseStorage.Core.Tests.Organize.PlanAssert;

/// <summary>
/// S-A-001 (AC-1, AC-8, AC-9; mixed-chest-category-contracts.md 5.4, 6.8 and 6.9): the coordinator's Organize branch reads the setting
/// once per start, with false it gives the v0.1 plan, and the Put away branch gives the same plan with the setting on and off.
/// </summary>
public sealed class MixedChestRunTests
{
    private static readonly BaseStart AtStation = BaseStart.FromStation(RunHarness.Station.Id);

    // GW-U-2403 (AC-1, 5.4): the Organize start reads the setting. A change after the start does not change that run, and the next start does.
    [Fact]
    public void TheOrganizeStartReadsTheSettingAndALaterChangeWaitsForTheNextStart()
    {
        RunHarness h = RunHarness.Make(autoAssignMixedChests: true);

        Assert.Equal(OrganizeStartOutcome.Started, h.Coordinator.Start(RunHarness.Inside));
        Assert.Equal(ChestRole.Category, h.Coordinator.Plan!.FindLayout(Id(209))!.Role);
        Assert.Equal("Wood", h.Coordinator.Plan!.AutoAssign.Find(Id(209))!.CategoryName);

        h.SettingsSource.WithBool(SettingKeys.AutoAssignMixedChests, false);
        Assert.Equal("Wood", h.Coordinator.Plan!.AutoAssign.Find(Id(209))!.CategoryName);

        Assert.True(h.Coordinator.Cancel());
        Assert.Equal(OrganizeStartOutcome.Started, h.Coordinator.Start(RunHarness.Inside));
        Assert.Equal(ChestRole.Source, h.Coordinator.Plan!.FindLayout(Id(209))!.Role);
    }

    // GW-U-2443 (AC-8, 5.4): with false the coordinator's Organize plan over base E is the v0.1 plan.
    [Fact]
    public void WithFalseTheCoordinatorGivesTheV01OrganizePlan()
    {
        RunHarness h = RunHarness.Make(autoAssignMixedChests: false);

        Assert.Equal(OrganizeStartOutcome.Started, h.Coordinator.Start(RunHarness.Inside));

        AssertSamePlan(Run(E()), h.Coordinator.Plan!);
        Assert.Equal(ChestRole.Source, h.Coordinator.Plan!.FindLayout(Id(209))!.Role);
        Assert.Equal(2, h.Coordinator.Plan!.NoRoom.Count);
    }

    // GW-U-2446 (AC-9, 6.9 row 3): Put away plans the same with the setting on and off.
    [Fact]
    public void PutAwayGivesTheSamePlanWhateverTheSetting()
    {
        RunHarness on = RunHarness.Make(autoAssignMixedChests: true);
        RunHarness off = RunHarness.Make(autoAssignMixedChests: false);

        Assert.Equal(OrganizeStartOutcome.Started, on.Coordinator.StartDeposit(AtStation));
        Assert.Equal(OrganizeStartOutcome.Started, off.Coordinator.StartDeposit(AtStation));

        Assert.Equal(MovesOf(off.Coordinator.Plan!), MovesOf(on.Coordinator.Plan!));
        Assert.Equal(NoRoomOf(off.Coordinator.Plan!), NoRoomOf(on.Coordinator.Plan!));
        Assert.Equal(LayoutsOf(off.Coordinator.Plan!), LayoutsOf(on.Coordinator.Plan!));
        Assert.Equal(ClaimedOf(off.Coordinator.Plan!), ClaimedOf(on.Coordinator.Plan!));
    }
}
