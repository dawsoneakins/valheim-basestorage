namespace BaseStorage.Core.Tests.Crafting;

using BaseStorage.Core.Availability;
using BaseStorage.Core.Tests.ChestAccess;
using BaseStorage.Plugin.Crafting;

/// <summary>GW-U-1114 and GW-U-1115 (AC-2, contract 5.3 CF8 marker and CF10 idle tick).</summary>
public sealed class CraftSessionMarkerTests
{
    // GW-U-1114 (CF8): the first-statement call sets the marker, returns true and changes nothing else.
    [Fact]
    public void TheFirstStatementSetsTheMarkerAndChangesNothingElse()
    {
        var rig = new CraftRig();

        Assert.True(rig.Session.OnDoCrafting(null, null));

        Assert.True(rig.Session.VanillaCraftRunning);
        Assert.Equal(CraftPhase.Idle, rig.Session.Phase);
        Assert.Equal(0, rig.Session.Token);
        Assert.Empty(rig.CraftGame.Calls);
        Assert.Empty(rig.CraftGame.Notices);
        Assert.False(rig.Session.CountsChests(rig.Recipe));
    }

    // GW-U-1114 (CF8): while Committing the same call returns true without setting the marker.
    [Fact]
    public void DuringACommitTheSameCallReturnsTrueWithoutSettingTheMarker()
    {
        var rig = new CraftRig();
        bool? result = null;
        bool? marker = null;

        rig.RunCommit(_ =>
        {
            result = rig.Session.OnDoCrafting(null, null);
            marker = rig.Session.VanillaCraftRunning;
            rig.Session.OnConsumeResources(true);
        });

        Assert.Equal(true, result);
        Assert.Equal(false, marker);
    }

    // GW-U-1114 (CF8): an Execute that throws leaves the marker set and the phase Idle.
    [Fact]
    public void AThrowingExecuteLeavesTheMarkerSetAndThePhaseIdle()
    {
        var rig = new CraftRig();
        rig.Reservations.FaultMember = nameof(FakeReservationGame.Observe);
        rig.Reservations.Fault = new InvalidOperationException("boom");
        ConsumptionPlan plan = rig.Plan() ?? throw new InvalidOperationException("The fixture has no plan.");

        Exception? thrown = Record.Exception(() => rig.Session.OnDoCrafting(rig.Selection, plan));

        Assert.IsType<InvalidOperationException>(thrown);
        Assert.True(rig.Session.VanillaCraftRunning);
        Assert.Equal(CraftPhase.Idle, rig.Session.Phase);

        rig.Session.Tick();

        Assert.False(rig.Session.VanillaCraftRunning);
    }

    // GW-U-1114 (CF8): Tick clears the marker.
    [Fact]
    public void TickClearsTheMarker()
    {
        var rig = new CraftRig();
        rig.Session.OnDoCrafting(null, null);

        rig.Session.Tick();

        Assert.False(rig.Session.VanillaCraftRunning);
    }

    // GW-U-1115 (CF10, S14): an idle tick makes no game call, writes no line and allocates nothing after warm-up.
    [Fact]
    public void AnIdleTickMakesNoGameCallWritesNoLineAndAllocatesNothing()
    {
        var rig = new CraftRig();

        rig.Session.Tick();
        rig.Session.Tick();

        Assert.Equal(CraftPhase.Idle, rig.Session.Phase);
        Assert.Empty(rig.CraftGame.Calls);
        Assert.Empty(rig.Sink.Entries);

        rig.Session.Tick();
        long before = GC.GetAllocatedBytesForCurrentThread();
        rig.Session.Tick();
        long after = GC.GetAllocatedBytesForCurrentThread();

        Assert.Equal(0, after - before);
    }
}
