namespace BaseStorage.Core.Tests.Fakes;

using System;
using BaseStorage.Core.Abstractions;
using BaseStorage.Core.Snapshots;
using static BaseStorage.Core.Tests.Fakes.FakeFixtures;

/// <summary>
/// AC-1: each injectable fault produces the layer's documented outcome and leaves container contents unchanged. Covers
/// the precedence rules, clearing, the argument checks of <c>InjectFault</c> and the manual reservation mode.
/// </summary>
public sealed class FakeContainerLayerFaultTests
{
    // GW-U-070
    [Fact]
    public void ReservationRefusedGivesPendingThenDeniedAndLeavesTheChestUnchanged()
    {
        FakeContainerLayer layer = FakeFixtures.NewLayer();
        ContainerSnapshot a = FakeFixtures.ChestA();
        layer.InjectFault(a.Id, FakeFault.ReservationRefused);

        layer.RequestReservation(a.Id);
        Assert.Equal(ReservationStatus.Pending, layer.GetReservationStatus(a.Id));
        Assert.Equal(ReservationStatus.Denied, layer.GetReservationStatus(a.Id));

        Assert.Equal(ContainerOperationResult.NotReserved, layer.AddItems(a.Id, Wood(3)));
        Assert.Equal(ContainerOperationResult.NotReserved, layer.RemoveFromStack(a.Id, 0, Ore(30), 30));
        Assert.Equal(a, Get(layer, a.Id));
    }

    // GW-U-070
    [Fact]
    public void ResolvePendingReservationsGivesDeniedWhenAutoResolveIsOff()
    {
        FakeContainerLayer layer = FakeFixtures.NewLayer();
        ContainerSnapshot a = FakeFixtures.ChestA();
        layer.AutoResolveReservations = false;
        layer.InjectFault(a.Id, FakeFault.ReservationRefused);
        layer.RequestReservation(a.Id);

        layer.ResolvePendingReservations();

        Assert.Equal(ReservationStatus.Denied, layer.GetReservationStatus(a.Id));
    }

    // GW-U-070
    [Fact]
    public void AfterClearingTheFaultReleaseThenRequestIsGranted()
    {
        FakeContainerLayer layer = FakeFixtures.NewLayer();
        ContainerSnapshot a = FakeFixtures.ChestA();
        layer.InjectFault(a.Id, FakeFault.ReservationRefused);
        layer.RequestReservation(a.Id);
        Assert.Equal(ReservationStatus.Pending, layer.GetReservationStatus(a.Id));
        Assert.Equal(ReservationStatus.Denied, layer.GetReservationStatus(a.Id));

        layer.ClearFault(a.Id, FakeFault.ReservationRefused);
        layer.ReleaseReservation(a.Id);
        layer.RequestReservation(a.Id);

        Assert.Equal(ReservationStatus.Pending, layer.GetReservationStatus(a.Id));
        Assert.Equal(ReservationStatus.Granted, layer.GetReservationStatus(a.Id));
    }

    // GW-U-070
    [Fact]
    public void InjectingWhileGrantedDoesNotRevokeTheReservation()
    {
        FakeContainerLayer layer = FakeFixtures.NewLayer();
        ContainerSnapshot a = FakeFixtures.ChestA();
        Grant(layer, a.Id);

        layer.InjectFault(a.Id, FakeFault.ReservationRefused);

        Assert.Equal(ReservationStatus.Granted, layer.GetReservationStatus(a.Id));
    }

    // GW-U-070
    [Fact]
    public void InjectingReservationRefusedOnTheInventoryThrows()
    {
        FakeContainerLayer layer = FakeFixtures.NewLayer();
        ContainerId inventory = FakeFixtures.Inventory().Id;

        Assert.Throws<ArgumentException>("fault", () => layer.InjectFault(inventory, FakeFault.ReservationRefused));
    }

    // GW-U-071
    [Fact]
    public void ChestChangedGivesContainerChangedAndLeavesTheChestUnchanged()
    {
        FakeContainerLayer layer = FakeFixtures.NewLayer();
        ContainerSnapshot a = FakeFixtures.ChestA();
        Grant(layer, a.Id);
        layer.InjectFault(a.Id, FakeFault.ChestChanged);

        Assert.Equal(ContainerOperationResult.ContainerChanged, layer.RemoveFromStack(a.Id, 0, Ore(30), 30));
        Assert.Equal(a, Get(layer, a.Id));
    }

    // GW-U-071
    [Fact]
    public void ChestChangedDoesNotAffectAdd()
    {
        FakeContainerLayer layer = FakeFixtures.NewLayer();
        ContainerSnapshot a = FakeFixtures.ChestA();
        Grant(layer, a.Id);
        layer.InjectFault(a.Id, FakeFault.ChestChanged);

        Assert.Equal(ContainerOperationResult.Success, layer.AddItems(a.Id, Wood(3)));
        Assert.Equal(new[] { Ore(30), Wood(20) }, Get(layer, a.Id).Contents);
    }

    // GW-U-071
    [Fact]
    public void NotReservedTakesPrecedenceOverChestChangedAndClearingRestoresRemove()
    {
        FakeContainerLayer layer = FakeFixtures.NewLayer();
        ContainerSnapshot b = FakeFixtures.ChestB();
        layer.InjectFault(b.Id, FakeFault.ChestChanged);

        Assert.Equal(ContainerOperationResult.NotReserved, layer.RemoveFromStack(b.Id, 0, Ore(12), 12));

        layer.ClearFault(b.Id, FakeFault.ChestChanged);
        Grant(layer, b.Id);
        Assert.Equal(ContainerOperationResult.Success, layer.RemoveFromStack(b.Id, 0, Ore(12), 12));
    }

    // GW-U-071
    [Fact]
    public void ChestChangedIsAllowedOnTheInventory()
    {
        ItemStackSnapshot stone = new("Stone", 20, 1, 50);
        ContainerSnapshot inventory = FakeFixtures.Inventory(stone);
        FakeContainerLayer layer = new(inventory);
        layer.InjectFault(inventory.Id, FakeFault.ChestChanged);

        Assert.Equal(ContainerOperationResult.ContainerChanged, layer.RemoveFromStack(inventory.Id, 0, stone, 20));
        Assert.Equal(inventory, Get(layer, inventory.Id));
    }

    // GW-U-072
    [Fact]
    public void DestinationFullGivesDestinationFullAndLeavesTheChestUnchanged()
    {
        FakeContainerLayer layer = FakeFixtures.NewLayer();
        ContainerSnapshot b = FakeFixtures.ChestB();
        Grant(layer, b.Id);
        layer.InjectFault(b.Id, FakeFault.DestinationFull);

        Assert.Equal(ContainerOperationResult.DestinationFull, layer.AddItems(b.Id, Ore(1)));
        Assert.Equal(b, Get(layer, b.Id));

        layer.ClearFault(b.Id, FakeFault.DestinationFull);
        Assert.Equal(ContainerOperationResult.Success, layer.AddItems(b.Id, Ore(1)));
    }

    // GW-U-072
    [Fact]
    public void DestinationFullDoesNotAffectRemove()
    {
        FakeContainerLayer layer = FakeFixtures.NewLayer();
        ContainerSnapshot b = FakeFixtures.ChestB();
        Grant(layer, b.Id);
        layer.InjectFault(b.Id, FakeFault.DestinationFull);

        Assert.Equal(ContainerOperationResult.Success, layer.RemoveFromStack(b.Id, 0, Ore(12), 12));
        Assert.Empty(Get(layer, b.Id).Contents);
    }

    // GW-U-072
    [Fact]
    public void NotReservedTakesPrecedenceOverDestinationFull()
    {
        FakeContainerLayer layer = FakeFixtures.NewLayer();
        ContainerSnapshot a = FakeFixtures.ChestA();
        layer.InjectFault(a.Id, FakeFault.DestinationFull);

        Assert.Equal(ContainerOperationResult.NotReserved, layer.AddItems(a.Id, Ore(1)));
        Assert.Equal(a, Get(layer, a.Id));
    }

    // GW-U-072
    [Fact]
    public void DestinationFullOnTheInventoryLeavesItUnchanged()
    {
        ItemStackSnapshot stone = new("Stone", 1, 1, 50);
        ContainerSnapshot inventory = FakeFixtures.Inventory();
        FakeContainerLayer layer = new(inventory);
        layer.InjectFault(inventory.Id, FakeFault.DestinationFull);

        Assert.Equal(ContainerOperationResult.DestinationFull, layer.AddItems(inventory.Id, stone));
        Assert.Equal(inventory, Get(layer, inventory.Id));
    }

    // GW-U-073
    [Theory]
    [InlineData(ReservationStatus.Granted)]
    [InlineData(ReservationStatus.Pending)]
    public void OwnerDisconnectMakesGrantedOrPendingLostImmediately(ReservationStatus start)
    {
        FakeContainerLayer layer = FakeFixtures.NewLayer();
        ContainerSnapshot a = FakeFixtures.ChestA();
        Reach(layer, a.Id, start);

        layer.InjectFault(a.Id, FakeFault.OwnerDisconnect);

        Assert.Equal(ReservationStatus.Lost, layer.GetReservationStatus(a.Id));
        Assert.Equal(a, Get(layer, a.Id));
        Assert.Equal(ContainerOperationResult.NotReserved, layer.AddItems(a.Id, Wood(3)));
        Assert.Equal(ContainerOperationResult.NotReserved, layer.RemoveFromStack(a.Id, 0, Ore(30), 30));
        Assert.Equal(a, Get(layer, a.Id));
    }

    // GW-U-073
    [Fact]
    public void OwnerDisconnectLeavesNoneAndDeniedUnchanged()
    {
        FakeContainerLayer layer = FakeFixtures.NewLayer();
        ContainerSnapshot a = FakeFixtures.ChestA();
        ContainerSnapshot b = FakeFixtures.ChestB();

        layer.InjectFault(a.Id, FakeFault.OwnerDisconnect);
        Assert.Equal(ReservationStatus.None, layer.GetReservationStatus(a.Id));

        layer.InjectFault(b.Id, FakeFault.ReservationRefused);
        layer.RequestReservation(b.Id);
        Assert.Equal(ReservationStatus.Pending, layer.GetReservationStatus(b.Id));
        Assert.Equal(ReservationStatus.Denied, layer.GetReservationStatus(b.Id));

        layer.InjectFault(b.Id, FakeFault.OwnerDisconnect);
        Assert.Equal(ReservationStatus.Denied, layer.GetReservationStatus(b.Id));
    }

    // GW-U-073
    [Fact]
    public void WhileArmedARequestResolvesToLostAndClearingRestoresAGrant()
    {
        FakeContainerLayer layer = FakeFixtures.NewLayer();
        ContainerSnapshot a = FakeFixtures.ChestA();
        layer.InjectFault(a.Id, FakeFault.OwnerDisconnect);

        layer.RequestReservation(a.Id);
        Assert.Equal(ReservationStatus.Pending, layer.GetReservationStatus(a.Id));
        Assert.Equal(ReservationStatus.Lost, layer.GetReservationStatus(a.Id));

        layer.ClearFault(a.Id, FakeFault.OwnerDisconnect);
        layer.RequestReservation(a.Id);
        Assert.Equal(ReservationStatus.Pending, layer.GetReservationStatus(a.Id));
        Assert.Equal(ReservationStatus.Granted, layer.GetReservationStatus(a.Id));
    }

    // GW-U-073
    [Fact]
    public void InjectingOwnerDisconnectOnTheInventoryThrows()
    {
        FakeContainerLayer layer = FakeFixtures.NewLayer();
        ContainerId inventory = FakeFixtures.Inventory().Id;

        Assert.Throws<ArgumentException>("fault", () => layer.InjectFault(inventory, FakeFault.OwnerDisconnect));
    }

    // GW-U-073
    [Fact]
    public void InjectingOnAnUnknownIdThrows()
    {
        FakeContainerLayer layer = FakeFixtures.NewLayer();

        Assert.Throws<ArgumentException>("id", () => layer.InjectFault(new ContainerId(1L, 1u), FakeFault.ChestChanged));
    }

    // GW-U-073
    [Fact]
    public void AnUndefinedFaultValueThrows()
    {
        FakeContainerLayer layer = FakeFixtures.NewLayer();
        ContainerId a = FakeFixtures.ChestA().Id;

        Assert.Throws<ArgumentOutOfRangeException>("fault", () => layer.InjectFault(a, (FakeFault)99));
    }

    // GW-U-073
    [Fact]
    public void InjectingTwiceAndClearingAnUnarmedFaultDoNotThrow()
    {
        FakeContainerLayer layer = FakeFixtures.NewLayer();
        ContainerId a = FakeFixtures.ChestA().Id;

        Assert.Null(Record.Exception(() => layer.InjectFault(a, FakeFault.ChestChanged)));
        Assert.Null(Record.Exception(() => layer.InjectFault(a, FakeFault.ChestChanged)));
        Assert.Null(Record.Exception(() => layer.ClearFault(a, FakeFault.DestinationFull)));
    }

    // GW-U-073
    [Fact]
    public void ManualModeKeepsPendingUntilResolvePendingReservations()
    {
        FakeContainerLayer layer = FakeFixtures.NewLayer();
        ContainerSnapshot a = FakeFixtures.ChestA();
        layer.AutoResolveReservations = false;
        layer.RequestReservation(a.Id);

        for (int poll = 0; poll < 3; poll++)
        {
            Assert.Equal(ReservationStatus.Pending, layer.GetReservationStatus(a.Id));
        }

        layer.ResolvePendingReservations();

        Assert.Equal(ReservationStatus.Granted, layer.GetReservationStatus(a.Id));
    }

    private static void Reach(FakeContainerLayer layer, ContainerId id, ReservationStatus status)
    {
        switch (status)
        {
            case ReservationStatus.Pending:
                layer.RequestReservation(id);
                break;
            case ReservationStatus.Granted:
                Grant(layer, id);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(status));
        }
    }
}
