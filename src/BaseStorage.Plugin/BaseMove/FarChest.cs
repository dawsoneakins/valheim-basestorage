namespace BaseStorage.Plugin.BaseMove;

using System;
using System.Collections.Generic;
using BaseStorage.Core.BaseMove;
using BaseStorage.Core.Snapshots;
using UnityEngine;

/// <summary>
/// One chest decoded from its record into a temporary inventory the world never sees, exactly as <c>Container.Load</c> does. The game
/// silently drops an item it cannot add when it loads, such as one from a removed mod, so the saved item count is compared with what
/// arrived; a chest that lost anything in the read is unreadable and is never rewritten. Main thread only.
/// </summary>
internal sealed class FarChest
{
    // Inventory.Load reads a ushort count from this save version up, and an int count before it.
    private const int UshortCountVersion = 108;

    private FarChest(FarChestRecord record, Inventory inventory, ContainerSnapshot snapshot)
    {
        Record = record;
        Inventory = inventory;
        Snapshot = snapshot;
        Items = new List<ItemDrop.ItemData>(inventory.GetAllItems());
    }

    internal FarChestRecord Record { get; }

    internal Inventory Inventory { get; }

    internal ContainerSnapshot Snapshot { get; }

    /// <summary>Gets the items in snapshot order, fixed at the read: a plan's stack index points here.</summary>
    internal List<ItemDrop.ItemData> Items { get; }

    /// <summary>Reads one chest.</summary>
    /// <param name="record">The chest's record.</param>
    /// <param name="flags">The chest's flags.</param>
    /// <returns>The chest, or null when it cannot be read without losing anything.</returns>
    internal static FarChest? Read(FarChestRecord record, ContainerFlags flags)
    {
        Container prefab = record.Prefab;
        var inventory = new Inventory(prefab.m_name, null, prefab.m_width, prefab.m_height);
        byte[]? saved = record.Zdo.GetByteArray(ZDOVars.s_items, null);
        if (saved != null)
        {
            inventory.Load(new ZPackage(saved));
            if (SavedCount(saved) != inventory.GetAllItems().Count)
            {
                return null;
            }
        }

        ContainerSnapshot? snapshot = SnapshotOf(record, inventory, flags);
        return snapshot == null ? null : new FarChest(record, inventory, snapshot);
    }

    /// <summary>Takes a new snapshot of this chest's temporary inventory.</summary>
    /// <returns>The snapshot, or null when an item is not valid.</returns>
    internal ContainerSnapshot? SnapshotNow() => SnapshotOf(Record, Inventory, Snapshot.Flags);

    /// <summary>Encodes the temporary inventory the way <c>Container.Save</c> does. Writes nothing.</summary>
    /// <returns>The bytes.</returns>
    internal byte[] Encode()
    {
        var package = new ZPackage();
        Inventory.Save(package);
        return package.GetArray();
    }

    /// <summary>Reads how many slots a chest uses from its saved header, without decoding the items.</summary>
    /// <param name="record">The chest.</param>
    /// <returns>The used slots.</returns>
    internal static int UsedSlots(FarChestRecord record)
    {
        byte[]? saved = record.Zdo.GetByteArray(ZDOVars.s_items, null);
        return saved == null || saved.Length == 0 ? 0 : SavedCount(saved);
    }

    private static int SavedCount(byte[] saved)
    {
        var package = new ZPackage(saved);
        int version = package.ReadInt();
        return version >= UshortCountVersion ? package.ReadUShort() : package.ReadInt();
    }

    private static ContainerSnapshot? SnapshotOf(FarChestRecord record, Inventory inventory, ContainerFlags flags)
    {
        var stacks = new List<ItemStackSnapshot>();
        foreach (ItemDrop.ItemData item in inventory.GetAllItems())
        {
            if (item == null || item.m_dropPrefab == null || item.m_shared == null || item.m_stack < 1 || item.m_quality < 1 || item.m_shared.m_maxStackSize < 1)
            {
                return null;
            }

            stacks.Add(new ItemStackSnapshot(item.m_dropPrefab.name, item.m_stack, item.m_quality, item.m_shared.m_maxStackSize));
        }

        Vector3 point = record.Zdo.GetPosition();
        return new ContainerSnapshot(record.Id, new WorldPosition(point.x, point.y, point.z), inventory.GetWidth(), inventory.GetHeight(), stacks, flags);
    }
}
