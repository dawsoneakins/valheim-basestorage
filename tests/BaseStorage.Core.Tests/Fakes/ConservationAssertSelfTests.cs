namespace BaseStorage.Core.Tests.Fakes;

using System.Collections.Generic;
using BaseStorage.Core.Abstractions;
using BaseStorage.Core.Snapshots;
using static BaseStorage.Core.Tests.Fakes.FakeFixtures;

/// <summary>
/// AC-4: self-tests that show <see cref="ConservationAssert.Holds"/> fails on a lost item, a duplicated item and an
/// over-full stack, with the affected item named in the message, and passes on conserving operations run through the fake.
/// </summary>
public sealed class ConservationAssertSelfTests
{
    // GW-U-079
    [Fact]
    public void LostItemFailsAndTheMessageNamesTheItem()
    {
        FakeContainerLayer layer = FakeFixtures.NewLayer();
        ContainerSnapshot a = FakeFixtures.ChestA();
        IReadOnlyList<ContainerSnapshot> before = layer.Capture();
        Grant(layer, a.Id);
        Assert.Equal(ContainerOperationResult.Success, layer.RemoveFromStack(a.Id, 0, Ore(30), 30));

        Xunit.Sdk.XunitException ex = Assert.ThrowsAny<Xunit.Sdk.XunitException>(
            () => ConservationAssert.Holds(before, layer.Capture()));

        Assert.Contains("CopperOre", ex.Message);
        Assert.Contains("lost 30", ex.Message);
    }

    // GW-U-080
    [Fact]
    public void DuplicatedItemFailsAndTheMessageNamesTheItem()
    {
        FakeContainerLayer layer = FakeFixtures.NewLayer();
        ContainerSnapshot b = FakeFixtures.ChestB();
        IReadOnlyList<ContainerSnapshot> before = layer.Capture();
        Grant(layer, b.Id);
        Assert.Equal(ContainerOperationResult.Success, layer.AddItems(b.Id, Ore(30)));

        Xunit.Sdk.XunitException ex = Assert.ThrowsAny<Xunit.Sdk.XunitException>(
            () => ConservationAssert.Holds(before, layer.Capture()));

        Assert.Contains("CopperOre", ex.Message);
        Assert.Contains("duplicated 30", ex.Message);
    }

    // GW-U-081
    [Fact]
    public void OverFullStackFailsOnlyOnTheLimit()
    {
        ContainerSnapshot before = WithContents(FakeFixtures.ChestA(), Ore(30), Wood(50), Wood(25));
        ContainerSnapshot after = WithContents(FakeFixtures.ChestA(), Ore(30), Wood(75));

        Xunit.Sdk.XunitException ex = Assert.ThrowsAny<Xunit.Sdk.XunitException>(
            () => ConservationAssert.Holds(new[] { before }, new[] { after }));

        Assert.Contains("Wood", ex.Message);
        Assert.Contains("75/50", ex.Message);
        Assert.DoesNotContain("lost", ex.Message);
        Assert.DoesNotContain("duplicated", ex.Message);
    }

    // GW-U-082
    [Fact]
    public void ContractStepsFourAndFivePassThroughTheFake()
    {
        FakeContainerLayer layer = FakeFixtures.NewLayer();
        ContainerSnapshot a = FakeFixtures.ChestA();
        ContainerSnapshot b = FakeFixtures.ChestB();
        Grant(layer, a.Id);
        Grant(layer, b.Id);
        IReadOnlyList<ContainerSnapshot> before = layer.Capture();

        Assert.Equal(ContainerOperationResult.Success, layer.AddItems(b.Id, Ore(30)));
        Assert.Equal(ContainerOperationResult.Success, layer.RemoveFromStack(a.Id, 0, Ore(30), 30));

        ConservationAssert.Holds(before, layer.Capture());
    }

    // GW-U-082
    [Fact]
    public void InventoryDepositThroughTheFakePasses()
    {
        ItemStackSnapshot stone = new("Stone", 20, 1, 50);
        ContainerId inventory = FakeFixtures.Inventory().Id;
        ContainerId b = FakeFixtures.ChestB().Id;
        FakeContainerLayer layer = new(FakeFixtures.Inventory(stone));
        layer.AddChest(FakeFixtures.ChestB());
        Grant(layer, b);
        IReadOnlyList<ContainerSnapshot> before = layer.Capture();

        Assert.Equal(ContainerOperationResult.Success, layer.RemoveFromStack(inventory, 0, stone, 20));
        Assert.Equal(ContainerOperationResult.Success, layer.AddItems(b, stone));

        ConservationAssert.Holds(before, layer.Capture());
    }

}
