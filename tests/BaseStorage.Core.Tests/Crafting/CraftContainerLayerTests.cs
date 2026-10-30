namespace BaseStorage.Core.Tests.Crafting;

using BaseStorage.Core.Abstractions;
using BaseStorage.Core.Snapshots;
using BaseStorage.Core.Tests.ChestAccess;
using BaseStorage.Plugin.Crafting;

/// <summary>GW-U-1103 to GW-U-1107 (AC-2 and AC-4, contract 4.2 L1-L9): the craft container layer over the reservation adapter.</summary>
public sealed class CraftContainerLayerTests
{
    // GW-U-1103 (L1): the player inventory reads Granted with no driver or game call.
    [Fact]
    public void TheInventoryReadsGrantedWithoutAnyCall()
    {
        var rig = new CraftRig();

        Assert.Equal(ReservationStatus.Granted, rig.StatusOf(CraftFixtures.Inventory));
        Assert.Empty(rig.Reservations.Calls);
        Assert.Empty(rig.World.Calls);
    }

    // GW-U-1103 (L2): requesting a chest sends one open request and reads Pending.
    [Fact]
    public void RequestingAChestSendsOneOpenRequestAndReadsPending()
    {
        var rig = new CraftRig();

        rig.Layer.RequestReservation(CraftFixtures.C7);

        Assert.Single(rig.Reservations.Calls, call => call.StartsWith("SendOpenRequest", StringComparison.Ordinal));
        Assert.Equal(ReservationStatus.Pending, rig.StatusOf(CraftFixtures.C7));
    }

    // GW-U-1103 (L3): after the grant with owner Me the status calls BeginWrite and reads Granted, the driver is Writing.
    [Fact]
    public void AfterTheGrantTheStatusBeginsTheWriteAndReadsGranted()
    {
        var rig = new CraftRig();
        rig.Layer.RequestReservation(CraftFixtures.C7);

        rig.Grant(rig.Chest7);

        Assert.Equal(ReservationStatus.Granted, rig.StatusOf(CraftFixtures.C7));
        Assert.True(rig.Driver.CanWrite(CraftFixtures.C7));
    }

    // GW-U-1103 (L5): RemoveFromStack returns world.Take's result, and the operation is "craft".
    [Fact]
    public void RemoveFromStackReturnsTheWorldTakeResult()
    {
        var rig = new CraftRig();
        rig.Layer.RequestReservation(CraftFixtures.C7);
        rig.Grant(rig.Chest7);
        rig.StatusOf(CraftFixtures.C7);

        ContainerOperationResult result = rig.Layer.RemoveFromStack(CraftFixtures.C7, 0, CraftFixtures.Wood(10), 4);

        Assert.Equal(ContainerOperationResult.Success, result);
        Assert.Equal(new[] { "Wood 6" }, rig.Describe(CraftFixtures.C7));
        Assert.Equal("craft", CraftContainerLayer<FakeChest>.Operation);
    }

    // GW-U-1104 (L6): an organize reservation reads Denied, release is a no-op and RemoveFromStack is NotReserved.
    [Fact]
    public void AnotherOperationsReservationIsNeverUsedReleasedOrWritten()
    {
        var rig = new CraftRig();
        rig.Chest7.Owner = CraftFixtures.Me;
        _ = rig.Driver.Begin(rig.Chest7, "organize");

        Assert.Equal(ReservationStatus.Denied, rig.StatusOf(CraftFixtures.C7));
        rig.Layer.ReleaseReservation(CraftFixtures.C7);
        Assert.NotNull(rig.Driver.Find(CraftFixtures.C7));
        Assert.Equal(ContainerOperationResult.NotReserved, rig.Layer.RemoveFromStack(CraftFixtures.C7, 0, CraftFixtures.Wood(10), 4));
        Assert.Equal(new[] { "Wood 10" }, rig.Describe(CraftFixtures.C7));
    }

    // GW-U-1104 (L8): releasing our own reservation completes it, sends SetInUse false and reads None.
    [Fact]
    public void ReleasingOurOwnReservationCompletesItAndClearsInUse()
    {
        var rig = new CraftRig();
        rig.Layer.RequestReservation(CraftFixtures.C7);
        rig.Grant(rig.Chest7);

        rig.Layer.ReleaseReservation(CraftFixtures.C7);

        Assert.Equal(ReservationStatus.None, rig.StatusOf(CraftFixtures.C7));
        Assert.Contains(rig.Reservations.Calls, call => call.StartsWith("SetInUse ", StringComparison.Ordinal) && call.EndsWith(" false", StringComparison.Ordinal));
    }

    // GW-U-1105: null world and null driver throw by name.
    [Fact]
    public void NullWorldAndDriverThrowByName()
    {
        var rig = new CraftRig();

        ArgumentNullException world = Assert.Throws<ArgumentNullException>(() => new CraftContainerLayer<FakeChest>(null!, rig.Driver));
        Assert.Equal("world", world.ParamName);
        ArgumentNullException driver = Assert.Throws<ArgumentNullException>(() => new CraftContainerLayer<FakeChest>(rig.World, null!));
        Assert.Equal("driver", driver.ParamName);
    }

    // GW-U-1105: GetContainers is empty and read-only; GetContainer returns world.Read.
    [Fact]
    public void GetContainersIsEmptyAndGetContainerReadsTheWorld()
    {
        var rig = new CraftRig();

        Assert.Empty(rig.Layer.GetContainers());
        Assert.False(rig.Layer.GetContainers() is IList<ContainerSnapshot>);
        Assert.Equal(rig.World.Read(CraftFixtures.C7), rig.Layer.GetContainer(CraftFixtures.C7));
    }

    // GW-U-1105: PlayerInventoryId is read from the world on every call.
    [Fact]
    public void PlayerInventoryIdIsReadFromTheWorldOnEveryCall()
    {
        var rig = new CraftRig();
        Assert.Equal(CraftFixtures.Inventory, rig.Layer.PlayerInventoryId);

        rig.World.InventoryId = new ContainerId(CraftFixtures.Me, 9u);

        Assert.Equal(new ContainerId(CraftFixtures.Me, 9u), rig.Layer.PlayerInventoryId);
    }

    // GW-U-1105: AddItems(null) throws by name, and RemoveFromStack argument checks throw before any world call.
    [Fact]
    public void AddItemsNullAndRemoveArgumentChecksThrowBeforeAnyWorldCall()
    {
        var rig = new CraftRig();

        ArgumentNullException items = Assert.Throws<ArgumentNullException>(() => rig.Layer.AddItems(CraftFixtures.C7, null!));
        Assert.Equal("items", items.ParamName);
        Assert.ThrowsAny<ArgumentException>(() => rig.Layer.RemoveFromStack(CraftFixtures.C7, 0, CraftFixtures.Wood(1), 0));
        Assert.ThrowsAny<ArgumentException>(() => rig.Layer.RemoveFromStack(CraftFixtures.C7, -1, CraftFixtures.Wood(1), 1));
        Assert.Empty(rig.World.Calls);
    }

    // GW-U-1107 (L4): a grant lost to Erik reads Lost; RemoveFromStack and AddItems return NotReserved without calling the world.
    [Fact]
    public void AGrantLostToErikIsNotWrittenThrough()
    {
        var rig = new CraftRig();
        rig.Layer.RequestReservation(CraftFixtures.C7);
        rig.Grant(rig.Chest7);
        rig.Chest7.Owner = CraftFixtures.Erik;

        Assert.Equal(ReservationStatus.Lost, rig.StatusOf(CraftFixtures.C7));
        Assert.Equal(ContainerOperationResult.NotReserved, rig.Layer.RemoveFromStack(CraftFixtures.C7, 0, CraftFixtures.Wood(10), 4));
        Assert.Equal(ContainerOperationResult.NotReserved, rig.Layer.AddItems(CraftFixtures.C7, CraftFixtures.Wood(1)));
        Assert.Empty(rig.World.Calls);
    }

    // GW-U-1107 (L7): a chest Erik has open reads Denied (driver Failed InUseByOther).
    [Fact]
    public void AChestOpenByErikReadsDenied()
    {
        var rig = new CraftRig();
        rig.Chest7.InUseFlag = true;

        rig.Layer.RequestReservation(CraftFixtures.C7);

        Assert.Equal(ReservationStatus.Denied, rig.StatusOf(CraftFixtures.C7));
    }

    // GW-U-1107 (L9): an unknown chest makes RequestReservation a no-op and reads None.
    [Fact]
    public void AnUnknownChestIsANoOpAndReadsNone()
    {
        var rig = new CraftRig();
        var missing = new ContainerId(CraftFixtures.Me, 77u);

        rig.Layer.RequestReservation(missing);

        Assert.Equal(ReservationStatus.None, rig.StatusOf(missing));
        Assert.Null(rig.Driver.Find(missing));
    }

    // GW-U-1107 (L9): a write to a chest that vanishes after the grant is NotFound.
    [Fact]
    public void AWriteIsNotFoundOnlyAfterAGrant()
    {
        var rig = new CraftRig();
        rig.Layer.RequestReservation(CraftFixtures.C7);
        rig.Grant(rig.Chest7);
        rig.World.Unload(CraftFixtures.C7);

        Assert.Equal(ContainerOperationResult.NotFound, rig.Layer.RemoveFromStack(CraftFixtures.C7, 0, CraftFixtures.Wood(10), 4));
    }
}
