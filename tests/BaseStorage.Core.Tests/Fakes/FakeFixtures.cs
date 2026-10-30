namespace BaseStorage.Core.Tests.Fakes;

using System;
using BaseStorage.Core.Abstractions;
using BaseStorage.Core.Snapshots;
using Xunit;

/// <summary>
/// Shared fixture data for the fake container layer tests. Holds data only: contract 2.6 chest <c>a</c>, contract 3.3
/// chest <c>b</c> and the player inventory. Every accessor returns a new instance, so no test can change another's data.
/// </summary>
public static class FakeFixtures
{
    private const long UserId = 3141592653L;

    /// <summary>Returns contract 2.6 chest <c>a</c>: <c>3141592653:42</c>, 5x2, CopperOre 30/30 and Wood 17/50, PlayerBuilt.</summary>
    /// <returns>A new snapshot of chest <c>a</c>.</returns>
    public static ContainerSnapshot ChestA() => new(
        new ContainerId(UserId, 42u),
        new WorldPosition(14f, 31.25f, -836.5f),
        5,
        2,
        new[] { new ItemStackSnapshot("CopperOre", 30, 1, 30), new ItemStackSnapshot("Wood", 17, 1, 50) },
        ContainerFlags.PlayerBuilt);

    /// <summary>Returns contract 3.3 chest <c>b</c>: <c>3141592653:57</c>, 5x2, CopperOre 12/30, PlayerBuilt.</summary>
    /// <returns>A new snapshot of chest <c>b</c>.</returns>
    public static ContainerSnapshot ChestB() => new(
        new ContainerId(UserId, 57u),
        new WorldPosition(18f, 31.25f, -836.5f),
        5,
        2,
        new[] { new ItemStackSnapshot("CopperOre", 12, 1, 30) },
        ContainerFlags.PlayerBuilt);

    /// <summary>Returns the player inventory: <c>3141592653:7</c>, 8x4, flags None, with the given stacks.</summary>
    /// <param name="contents">The stacks in inventory order; none gives an empty inventory.</param>
    /// <returns>A new snapshot of the player inventory.</returns>
    public static ContainerSnapshot Inventory(params ItemStackSnapshot[] contents) => new(
        new ContainerId(UserId, 7u),
        new WorldPosition(0f, 0f, 0f),
        8,
        4,
        contents,
        ContainerFlags.None);

    /// <summary>
    /// Returns a layer with an empty player inventory and chest <c>b</c> added before chest <c>a</c>, so that the
    /// insertion order differs from the id order. Chests <c>a</c> and <c>b</c> are not reserved.
    /// </summary>
    /// <returns>A new fake layer seeded with the inventory and both chests.</returns>
    public static FakeContainerLayer NewLayer()
    {
        FakeContainerLayer layer = new(Inventory());
        layer.AddChest(ChestB());
        layer.AddChest(ChestA());
        return layer;
    }

    /// <summary>Returns a CopperOre stack (quality 1, max stack 30).</summary>
    /// <param name="count">The stack count.</param>
    /// <returns>A new stack.</returns>
    public static ItemStackSnapshot Ore(int count) => new("CopperOre", count, 1, 30);

    /// <summary>Returns a Wood stack (quality 1, max stack 50).</summary>
    /// <param name="count">The stack count.</param>
    /// <returns>A new stack.</returns>
    public static ItemStackSnapshot Wood(int count) => new("Wood", count, 1, 50);

    /// <summary>Returns a Stone stack (quality 1, max stack 50).</summary>
    /// <param name="count">The stack count.</param>
    /// <returns>A new stack.</returns>
    public static ItemStackSnapshot Stone(int count) => new("Stone", count, 1, 50);

    /// <summary>Returns the container as the layer holds it now.</summary>
    /// <param name="layer">The layer to read.</param>
    /// <param name="id">The container id.</param>
    /// <returns>The current snapshot.</returns>
    /// <exception cref="InvalidOperationException">The container is not loaded in the fake.</exception>
    public static ContainerSnapshot Get(FakeContainerLayer layer, ContainerId id) =>
        layer.GetContainer(id) ?? throw new InvalidOperationException("The container is not loaded in the fake.");

    /// <summary>Requests a reservation and resolves it to Granted, asserting the Pending then Granted sequence.</summary>
    /// <param name="layer">The layer to reserve in.</param>
    /// <param name="id">The container id.</param>
    public static void Grant(FakeContainerLayer layer, ContainerId id)
    {
        layer.RequestReservation(id);
        Assert.Equal(ReservationStatus.Pending, layer.GetReservationStatus(id));
        Assert.Equal(ReservationStatus.Granted, layer.GetReservationStatus(id));
    }

    /// <summary>Returns a copy of the template with different contents.</summary>
    /// <param name="template">The container to copy.</param>
    /// <param name="contents">The new stacks.</param>
    /// <returns>A new snapshot.</returns>
    public static ContainerSnapshot WithContents(ContainerSnapshot template, params ItemStackSnapshot[] contents) => new(
        template.Id,
        template.Position,
        template.Width,
        template.Height,
        contents,
        template.Flags);
}
