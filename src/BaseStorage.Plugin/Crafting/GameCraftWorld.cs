namespace BaseStorage.Plugin.Crafting;

using System;
using System.Collections.Generic;
using BaseStorage.Core.Abstractions;
using BaseStorage.Core.Snapshots;
using BaseStorage.Plugin.World;
using UnityEngine;

/// <summary>
/// The game-bound world port of a craft (contract craft-from-base 7.2). It reads the player inventory and the loaded chests,
/// takes stacks with the vanilla Inventory calls while keeping a clone of each, and puts the clones back. It never checks
/// reservations: the container layer does. Main thread only (W2).
/// </summary>
internal sealed class GameCraftWorld : ICraftWorld<Container>
{
    private readonly List<KeptStack> kept = new List<KeptStack>();
    private readonly List<Container> scratch = new List<Container>();

    /// <inheritdoc />
    public ContainerId InventoryId
    {
        get
        {
            long session = ZDOMan.GetSessionID();
            return Player.m_localPlayer != null && session != 0L ? new ContainerId(session, 0u) : default;
        }
    }

    /// <inheritdoc />
    public Container? FindChest(ContainerId id)
    {
        PluginWorld? world = PluginWorld.Current;
        if (world == null || id.IsNone)
        {
            return null;
        }

        world.Containers.CopyItemsTo(scratch);
        Container? found = null;
        for (int i = 0; i < scratch.Count && found == null; i++)
        {
            Container candidate = scratch[i];
            if (candidate == null || candidate.m_nview == null || !candidate.m_nview.IsValid())
            {
                continue;
            }

            ZDO zdo = candidate.m_nview.GetZDO();
            if (zdo == null)
            {
                continue;
            }

            ZDOID uid = zdo.m_uid;
            if (uid.UserID == id.UserId && uid.ID == id.Id)
            {
                found = candidate;
            }
        }

        scratch.Clear();
        return found;
    }

    /// <inheritdoc />
    public ContainerSnapshot? Read(ContainerId id)
    {
        if (id.IsNone)
        {
            return null;
        }

        if (id == InventoryId)
        {
            return ReadInventory();
        }

        PluginWorld? world = PluginWorld.Current;
        if (world == null)
        {
            return null;
        }

        IReadOnlyList<ContainerSnapshot> all = world.Snapshots.GetContainers();
        for (int i = 0; i < all.Count; i++)
        {
            if (all[i].Id == id)
            {
                return all[i];
            }
        }

        return null;
    }

    /// <summary>Reads a fresh snapshot of the local player's inventory (contract 5.2).</summary>
    /// <returns>The snapshot, or null when there is no player or an item cannot be mapped.</returns>
    internal ContainerSnapshot? ReadInventory()
    {
        Player player = Player.m_localPlayer;
        ContainerId id = InventoryId;
        if (player == null || id.IsNone)
        {
            return null;
        }

        Inventory inventory = player.GetInventory();
        if (inventory == null)
        {
            return null;
        }

        List<ItemDrop.ItemData> items = inventory.GetAllItems();
        if (items == null)
        {
            return null;
        }

        var stacks = new List<ItemStackSnapshot>(items.Count);
        for (int i = 0; i < items.Count; i++)
        {
            ItemDrop.ItemData item = items[i];
            if (item == null || item.m_dropPrefab == null || item.m_shared == null || item.m_stack < 1 || item.m_quality < 1 || item.m_shared.m_maxStackSize < 1)
            {
                return null;
            }

            string name = item.m_dropPrefab.name;
            if (string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            stacks.Add(new ItemStackSnapshot(name, item.m_stack, item.m_quality, item.m_shared.m_maxStackSize));
        }

        Vector3 point = player.transform.position;
        int width = inventory.GetWidth();
        int height = inventory.GetHeight();
        if (width < 1 || height < 1)
        {
            return null;
        }

        try
        {
            return new ContainerSnapshot(id, new WorldPosition(point.x, point.y, point.z), width, height, stacks, ContainerFlags.None);
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    /// <inheritdoc />
    public ContainerOperationResult Take(ContainerId id, int stackIndex, ItemStackSnapshot expected, int count)
    {
        Inventory? inventory = InventoryOf(id);
        if (inventory == null)
        {
            return ContainerOperationResult.NotFound;
        }

        List<ItemDrop.ItemData> items = inventory.GetAllItems();
        if (items == null || stackIndex < 0 || stackIndex >= items.Count || !Matches(items[stackIndex], expected))
        {
            return ContainerOperationResult.ContainerChanged;
        }

        ItemDrop.ItemData item = items[stackIndex];
        ItemDrop.ItemData clone = item.Clone();
        clone.m_stack = count;
        if (!inventory.RemoveItem(item, count))
        {
            return ContainerOperationResult.ContainerChanged;
        }

        kept.Add(new KeptStack(id, clone));
        return ContainerOperationResult.Success;
    }

    /// <inheritdoc />
    public ContainerOperationResult PutBack(ContainerId id, ItemStackSnapshot items)
    {
        Inventory? target = InventoryOf(id);
        if (target == null)
        {
            return ContainerOperationResult.NotFound;
        }

        int index = IndexOfKept(id, items);
        if (index < 0)
        {
            index = IndexOfKept(default, items);
        }

        if (index < 0)
        {
            return ContainerOperationResult.DestinationFull;
        }

        ItemDrop.ItemData clone = kept[index].Clone;
        if (!target.CanAddItem(clone, clone.m_stack) || !target.AddItem(clone))
        {
            return ContainerOperationResult.DestinationFull;
        }

        kept.RemoveAt(index);
        return ContainerOperationResult.Success;
    }

    /// <inheritdoc />
    public int ReturnAllTaken()
    {
        int count = kept.Count;
        Player player = Player.m_localPlayer;
        for (int i = 0; i < count; i++)
        {
            ItemDrop.ItemData clone = kept[i].Clone;
            Inventory? inventory = player != null ? player.GetInventory() : null;
            if (inventory != null && inventory.CanAddItem(clone, clone.m_stack) && inventory.AddItem(clone))
            {
                continue;
            }

            Vector3 at = player != null ? player.transform.position : Vector3.zero;
            ItemDrop.DropItem(clone, clone.m_stack, at + Vector3.up, Quaternion.identity);
        }

        kept.Clear();
        return count;
    }

    /// <inheritdoc />
    public void ForgetTaken() => kept.Clear();

    private static bool Matches(ItemDrop.ItemData item, ItemStackSnapshot expected) =>
        item != null
        && item.m_dropPrefab != null
        && item.m_shared != null
        && item.m_stack == expected.Count
        && item.m_quality == expected.Quality
        && item.m_shared.m_maxStackSize == expected.MaxStack
        && string.Equals(item.m_dropPrefab.name, expected.PrefabName, StringComparison.Ordinal);

    private Inventory? InventoryOf(ContainerId id)
    {
        if (id == InventoryId)
        {
            Player player = Player.m_localPlayer;
            return player != null ? player.GetInventory() : null;
        }

        Container? chest = FindChest(id);
        return chest != null ? chest.m_inventory : null;
    }

    private int IndexOfKept(ContainerId id, ItemStackSnapshot items)
    {
        for (int i = 0; i < kept.Count; i++)
        {
            KeptStack candidate = kept[i];
            ItemDrop.ItemData clone = candidate.Clone;
            if ((!id.IsNone && candidate.Id != id)
                || clone.m_dropPrefab == null
                || !string.Equals(clone.m_dropPrefab.name, items.PrefabName, StringComparison.Ordinal)
                || clone.m_quality != items.Quality
                || clone.m_stack != items.Count)
            {
                continue;
            }

            return i;
        }

        return -1;
    }

    private readonly struct KeptStack
    {
        internal KeptStack(ContainerId id, ItemDrop.ItemData clone)
        {
            Id = id;
            Clone = clone;
        }

        internal ContainerId Id { get; }

        internal ItemDrop.ItemData Clone { get; }
    }
}
