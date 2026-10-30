namespace BaseStorage.Core.Tests.Fakes;

using System;
using BaseStorage.Core.Abstractions;
using BaseStorage.Core.Snapshots;
using static BaseStorage.Core.Tests.Fakes.FakeFixtures;

/// <summary>
/// AC-1: the fake's contract-faithful behaviour without faults. Covers the contract 3.3 worked example, seeding and reads,
/// reservation semantics and mutation checks. Fixtures are contract 2.6 chest <c>a</c>, contract 3.3 chest <c>b</c> and
/// the player inventory.
/// </summary>
public sealed class FakeContainerLayerTests
{
    // GW-U-066
    [Fact]
    public void ContractWorkedExampleHoldsStepByStep()
    {
        FakeContainerLayer layer = FakeFixtures.NewLayer();
        ContainerSnapshot a = FakeFixtures.ChestA();
        ContainerSnapshot b = FakeFixtures.ChestB();

        // Step 1: ordered by id although b was added first.
        Assert.Equal(new[] { a, b }, layer.GetContainers());

        // Step 2: not reserved, so nothing is added.
        Assert.Equal(ContainerOperationResult.NotReserved, layer.AddItems(b.Id, Ore(30)));
        Assert.Equal(b, Get(layer, b.Id));

        // Step 3: the first poll is Pending and the second is Granted, for both.
        layer.RequestReservation(a.Id);
        layer.RequestReservation(b.Id);
        Assert.Equal(ReservationStatus.Pending, layer.GetReservationStatus(a.Id));
        Assert.Equal(ReservationStatus.Pending, layer.GetReservationStatus(b.Id));
        Assert.Equal(ReservationStatus.Granted, layer.GetReservationStatus(a.Id));
        Assert.Equal(ReservationStatus.Granted, layer.GetReservationStatus(b.Id));

        // Step 4: the partial 12 is topped up to 30, then a new stack of 12 is appended.
        Assert.Equal(ContainerOperationResult.Success, layer.AddItems(b.Id, Ore(30)));
        Assert.Equal(new[] { Ore(30), Ore(12) }, Get(layer, b.Id).Contents);

        // Step 5: the copper ore stack is removed and the wood stack is left.
        Assert.Equal(ContainerOperationResult.Success, layer.RemoveFromStack(a.Id, 0, Ore(30), 30));
        Assert.Equal(new[] { Wood(17) }, Get(layer, a.Id).Contents);

        // Step 6: index 0 is now Wood 17, so the stale expectation is refused and a is unchanged.
        Assert.Equal(ContainerOperationResult.ContainerChanged, layer.RemoveFromStack(a.Id, 0, Ore(30), 30));
        Assert.Equal(new[] { Wood(17) }, Get(layer, a.Id).Contents);

        // Step 7: releasing both ends in None.
        layer.ReleaseReservation(a.Id);
        layer.ReleaseReservation(b.Id);
        Assert.Equal(ReservationStatus.None, layer.GetReservationStatus(a.Id));
        Assert.Equal(ReservationStatus.None, layer.GetReservationStatus(b.Id));

        // Step 8: re-reserve b. 10 slots cannot take 500 more wood, so the add is refused and b is unchanged.
        Grant(layer, b.Id);
        Assert.Equal(ContainerOperationResult.DestinationFull, layer.AddItems(b.Id, Wood(500)));
        Assert.Equal(new[] { Ore(30), Ore(12) }, Get(layer, b.Id).Contents);
    }

    // GW-U-067
    [Fact]
    public void PlayerInventoryIdIsTheInventoryId()
    {
        ContainerSnapshot inventory = FakeFixtures.Inventory();
        FakeContainerLayer layer = new(inventory);

        Assert.Equal(inventory.Id, layer.PlayerInventoryId);
    }

    // GW-U-067
    [Fact]
    public void LayerWithoutAnInventoryHasNoneAsPlayerInventoryId()
    {
        FakeContainerLayer layer = new();

        Assert.True(layer.PlayerInventoryId.IsNone);
    }

    // GW-U-067
    [Fact]
    public void GetContainersExcludesTheInventory()
    {
        FakeContainerLayer layer = FakeFixtures.NewLayer();

        Assert.Equal(new[] { FakeFixtures.ChestA(), FakeFixtures.ChestB() }, layer.GetContainers());
    }

    // GW-U-067
    [Fact]
    public void GetContainerReturnsTheInventoryAndNullForAnUnknownId()
    {
        FakeContainerLayer layer = FakeFixtures.NewLayer();
        ContainerSnapshot inventory = FakeFixtures.Inventory();

        Assert.Equal(inventory, layer.GetContainer(inventory.Id));
        Assert.Null(layer.GetContainer(new ContainerId(1L, 1u)));
    }

    // GW-U-067
    [Fact]
    public void GetContainerReturnsFreshEqualInstances()
    {
        FakeContainerLayer layer = FakeFixtures.NewLayer();
        ContainerSnapshot a = FakeFixtures.ChestA();

        ContainerSnapshot first = Get(layer, a.Id);
        ContainerSnapshot second = Get(layer, a.Id);

        Assert.NotSame(first, second);
        Assert.Equal(a, first);
        Assert.Equal(a, second);
    }

    // GW-U-067
    [Fact]
    public void CaptureIsTheInventoryFirstThenChestsById()
    {
        FakeContainerLayer layer = FakeFixtures.NewLayer();

        Assert.Equal(new[] { FakeFixtures.Inventory(), FakeFixtures.ChestA(), FakeFixtures.ChestB() }, layer.Capture());
    }

    // GW-U-067
    [Fact]
    public void CaptureWithoutAnInventoryIsTheChestsOnly()
    {
        FakeContainerLayer layer = new();
        layer.AddChest(FakeFixtures.ChestB());
        layer.AddChest(FakeFixtures.ChestA());

        Assert.Equal(new[] { FakeFixtures.ChestA(), FakeFixtures.ChestB() }, layer.Capture());
    }

    // GW-U-067 (seeding validation; passes against the red-first stub by design)
    [Fact]
    public void AddChestRejectsNull()
    {
        FakeContainerLayer layer = new();

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => layer.AddChest(null!));
        Assert.Equal("chest", ex.ParamName);
    }

    // GW-U-067 (seeding validation; passes against the red-first stub by design)
    [Fact]
    public void AddChestRejectsADuplicateId()
    {
        FakeContainerLayer layer = new();
        layer.AddChest(FakeFixtures.ChestA());

        Assert.Throws<ArgumentException>("chest", () => layer.AddChest(FakeFixtures.ChestA()));
    }

    // GW-U-067 (seeding validation; passes against the red-first stub by design)
    [Fact]
    public void AddChestRejectsAnIdThatReusesTheInventoryId()
    {
        FakeContainerLayer layer = new(FakeFixtures.Inventory());

        Assert.Throws<ArgumentException>("chest", () => layer.AddChest(FakeFixtures.Inventory()));
    }

    // GW-U-068
    [Fact]
    public void InventoryIsAlwaysGrantedAndIgnoresRequestAndRelease()
    {
        ContainerSnapshot inventory = FakeFixtures.Inventory();
        FakeContainerLayer layer = new(inventory);

        layer.RequestReservation(inventory.Id);
        layer.ReleaseReservation(inventory.Id);

        Assert.Equal(ReservationStatus.Granted, layer.GetReservationStatus(inventory.Id));
    }

    // GW-U-068
    [Fact]
    public void UnknownIdIsNoneAndRequestLeavesItNone()
    {
        FakeContainerLayer layer = FakeFixtures.NewLayer();
        ContainerId unknown = new(1L, 1u);

        Assert.Equal(ReservationStatus.None, layer.GetReservationStatus(unknown));
        layer.RequestReservation(unknown);
        Assert.Equal(ReservationStatus.None, layer.GetReservationStatus(unknown));
    }

    // GW-U-068
    [Fact]
    public void RequestWhilePendingOrGrantedIsANoOp()
    {
        FakeContainerLayer layer = FakeFixtures.NewLayer();
        ContainerSnapshot a = FakeFixtures.ChestA();

        layer.RequestReservation(a.Id);
        layer.RequestReservation(a.Id);
        Assert.Equal(ReservationStatus.Pending, layer.GetReservationStatus(a.Id));
        Assert.Equal(ReservationStatus.Granted, layer.GetReservationStatus(a.Id));

        layer.RequestReservation(a.Id);
        Assert.Equal(ReservationStatus.Granted, layer.GetReservationStatus(a.Id));
    }

    // GW-U-068
    [Theory]
    [InlineData(ReservationStatus.Pending)]
    [InlineData(ReservationStatus.Granted)]
    [InlineData(ReservationStatus.Denied)]
    [InlineData(ReservationStatus.Lost)]
    public void ReleaseFromAnyActiveStateEndsInNoneAndIsIdempotent(ReservationStatus start)
    {
        FakeContainerLayer layer = FakeFixtures.NewLayer();
        ContainerSnapshot a = FakeFixtures.ChestA();
        ReachStatus(layer, a.Id, start);

        layer.ReleaseReservation(a.Id);
        Assert.Equal(ReservationStatus.None, layer.GetReservationStatus(a.Id));

        layer.ReleaseReservation(a.Id);
        Assert.Equal(ReservationStatus.None, layer.GetReservationStatus(a.Id));
    }

    // GW-U-068
    [Fact]
    public void ReleaseOfAnUnknownIdDoesNotThrow()
    {
        FakeContainerLayer layer = FakeFixtures.NewLayer();

        Assert.Null(Record.Exception(() => layer.ReleaseReservation(new ContainerId(1L, 1u))));
    }

    // GW-U-068
    [Theory]
    [InlineData(ReservationStatus.Denied)]
    [InlineData(ReservationStatus.Lost)]
    public void RequestFromDeniedOrLostRestartsAtPending(ReservationStatus start)
    {
        FakeContainerLayer layer = FakeFixtures.NewLayer();
        ContainerSnapshot a = FakeFixtures.ChestA();
        ReachStatus(layer, a.Id, start);

        layer.RequestReservation(a.Id);

        Assert.Equal(ReservationStatus.Pending, layer.GetReservationStatus(a.Id));
    }

    // GW-U-069
    [Theory]
    [InlineData("AddNullItems", typeof(ArgumentNullException), "items")]
    [InlineData("RemoveNullExpected", typeof(ArgumentNullException), "expected")]
    [InlineData("RemoveNegativeIndex", typeof(ArgumentOutOfRangeException), "stackIndex")]
    [InlineData("RemoveZeroCount", typeof(ArgumentOutOfRangeException), "count")]
    [InlineData("RemoveCountAboveExpected", typeof(ArgumentOutOfRangeException), "count")]
    public void ArgumentChecksRunBeforeTheReservationCheck(string call, Type expectedType, string paramName)
    {
        // a is not reserved, so a reservation check would answer NotReserved if it ran first.
        FakeContainerLayer layer = FakeFixtures.NewLayer();
        ContainerSnapshot a = FakeFixtures.ChestA();

        Exception? actual = Record.Exception(() => Invoke(layer, a.Id, call));

        Assert.NotNull(actual);
        Assert.IsType(expectedType, actual);
        Assert.Equal(paramName, ((ArgumentException)actual).ParamName);
    }

    // GW-U-069
    [Fact]
    public void UnknownIdGivesNotFoundForBothMutationsEvenWhenUnreserved()
    {
        FakeContainerLayer layer = FakeFixtures.NewLayer();
        ContainerId unknown = new(1L, 1u);

        Assert.Equal(ContainerOperationResult.NotFound, layer.AddItems(unknown, Ore(30)));
        Assert.Equal(ContainerOperationResult.NotFound, layer.RemoveFromStack(unknown, 0, Ore(30), 30));
    }

    // GW-U-069
    [Fact]
    public void IndexAtOrBeyondTheContentsGivesContainerChanged()
    {
        FakeContainerLayer layer = FakeFixtures.NewLayer();
        ContainerSnapshot a = FakeFixtures.ChestA();
        Grant(layer, a.Id);

        Assert.Equal(ContainerOperationResult.ContainerChanged, layer.RemoveFromStack(a.Id, 2, Wood(17), 1));
        Assert.Equal(a, Get(layer, a.Id));
    }

    // GW-U-069
    [Fact]
    public void PartialRemoveLeavesTheRemainderAtTheSameIndex()
    {
        FakeContainerLayer layer = FakeFixtures.NewLayer();
        ContainerSnapshot a = FakeFixtures.ChestA();
        Grant(layer, a.Id);

        Assert.Equal(ContainerOperationResult.Success, layer.RemoveFromStack(a.Id, 1, Wood(17), 5));
        Assert.Equal(new[] { Ore(30), Wood(12) }, Get(layer, a.Id).Contents);
    }

    // GW-U-069
    [Fact]
    public void InventoryAcceptsAddAndRemoveWithoutAReservation()
    {
        ItemStackSnapshot stone = new("Stone", 20, 1, 50);
        ContainerSnapshot inventory = FakeFixtures.Inventory();
        FakeContainerLayer layer = new(inventory);

        Assert.Equal(ContainerOperationResult.Success, layer.AddItems(inventory.Id, stone));
        Assert.Equal(ContainerOperationResult.Success, layer.RemoveFromStack(inventory.Id, 0, stone, 20));
        Assert.Empty(Get(layer, inventory.Id).Contents);
    }

    // GW-U-069
    [Fact]
    public void AddIsAllOrNothingAtTheBoundary()
    {
        ContainerSnapshot fits = EmptyOneByOneChest(90u);
        ContainerSnapshot tooMuch = EmptyOneByOneChest(91u);
        FakeContainerLayer layer = new(FakeFixtures.Inventory());
        layer.AddChest(fits);
        layer.AddChest(tooMuch);
        Grant(layer, fits.Id);
        Grant(layer, tooMuch.Id);

        Assert.Equal(ContainerOperationResult.Success, layer.AddItems(fits.Id, Wood(50)));
        Assert.Equal(new[] { Wood(50) }, Get(layer, fits.Id).Contents);

        Assert.Equal(ContainerOperationResult.DestinationFull, layer.AddItems(tooMuch.Id, Wood(51)));
        Assert.Equal(tooMuch, Get(layer, tooMuch.Id));
    }

    // GW-U-069
    [Fact]
    public void TopUpThenAppendFillsTheExistingStackFirst()
    {
        FakeContainerLayer layer = FakeFixtures.NewLayer();
        ContainerSnapshot a = FakeFixtures.ChestA();
        Grant(layer, a.Id);

        Assert.Equal(ContainerOperationResult.Success, layer.AddItems(a.Id, Wood(40)));
        Assert.Equal(new[] { Ore(30), Wood(50), Wood(7) }, Get(layer, a.Id).Contents);
    }

    private static ContainerSnapshot EmptyOneByOneChest(uint serial) => new(
        new ContainerId(3141592653L, serial),
        new WorldPosition(0f, 0f, 0f),
        1,
        1,
        Array.Empty<ItemStackSnapshot>(),
        ContainerFlags.PlayerBuilt);

    private static void ReachStatus(FakeContainerLayer layer, ContainerId id, ReservationStatus status)
    {
        switch (status)
        {
            case ReservationStatus.Pending:
                layer.RequestReservation(id);
                break;
            case ReservationStatus.Granted:
                Grant(layer, id);
                break;
            case ReservationStatus.Denied:
                layer.InjectFault(id, FakeFault.ReservationRefused);
                layer.RequestReservation(id);
                Assert.Equal(ReservationStatus.Pending, layer.GetReservationStatus(id));
                Assert.Equal(ReservationStatus.Denied, layer.GetReservationStatus(id));
                break;
            case ReservationStatus.Lost:
                Grant(layer, id);
                layer.InjectFault(id, FakeFault.OwnerDisconnect);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(status));
        }
    }

    private static void Invoke(FakeContainerLayer layer, ContainerId id, string call)
    {
        switch (call)
        {
            case "AddNullItems":
                layer.AddItems(id, null!);
                break;
            case "RemoveNullExpected":
                layer.RemoveFromStack(id, 0, null!, 1);
                break;
            case "RemoveNegativeIndex":
                layer.RemoveFromStack(id, -1, Ore(30), 1);
                break;
            case "RemoveZeroCount":
                layer.RemoveFromStack(id, 0, Ore(30), 0);
                break;
            case "RemoveCountAboveExpected":
                layer.RemoveFromStack(id, 0, Ore(30), 31);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(call));
        }
    }
}
