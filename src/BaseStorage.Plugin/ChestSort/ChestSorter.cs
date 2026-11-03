namespace BaseStorage.Plugin.ChestSort;

using System;
using System.Collections.Generic;
using BaseStorage.Core.ChestSort;
using BaseStorage.Core.Reservations;
using BaseStorage.Core.Snapshots;
using BaseStorage.Core.Ui;
using BaseStorage.Plugin.CategoryButton;
using BaseStorage.Plugin.Categories;
using BaseStorage.Plugin.Reservations;
using BaseStorage.Plugin.Restock;
using BaseStorage.Plugin.Ui;

/// <summary>
/// Sorts the open chest through the reservation protocol. No item is created or copied: stacks keep their item objects, only counts and slots
/// change, and a chest with anything it cannot read is left alone. Nothing moves between chests. Main thread only.
/// </summary>
internal static class ChestSorter
{
    private const string Operation = "sort";

    private static ReservationDriver<Container>? driver;
    private static Container? chest;
    private static ContainerId id;

    /// <summary>Stores the reservation driver.</summary>
    /// <param name="reservations">The driver.</param>
    internal static void Initialize(ReservationDriver<Container> reservations) => driver = reservations;

    /// <summary>Starts sorting a chest. A sort already running keeps going and this press does nothing.</summary>
    /// <param name="target">The chest.</param>
    internal static void Start(Container target)
    {
        ZDO? zdo = target == null || target.m_nview == null || !target.m_nview.IsValid() ? null : target.m_nview.GetZDO();
        if (driver == null || chest != null || zdo == null || target == null)
        {
            return;
        }

        id = new ContainerId(zdo.m_uid.UserID, zdo.m_uid.ID);
        if (driver.Begin(target, Operation) == null)
        {
            Notices.Show(NoticePlacement.TopLeft, StringKeys.MessageChestSortBusy);
            return;
        }

        chest = target;
    }

    /// <summary>Runs the current sort once its chest is held. Called once per frame.</summary>
    internal static void Tick()
    {
        Container? current = chest;
        if (current == null || driver == null)
        {
            return;
        }

        Reservation<string>? reservation = driver.Find(id);
        if (reservation == null || !string.Equals(reservation.Operation, Operation, StringComparison.Ordinal))
        {
            chest = null;
            return;
        }

        if (reservation.State == ReservationState.Requesting)
        {
            return;
        }

        if (reservation.State == ReservationState.Granted)
        {
            driver.BeginWrite(id);
        }

        if (driver.CanWrite(id))
        {
            Sort(current);
        }
        else
        {
            Notices.Show(NoticePlacement.TopLeft, StringKeys.MessageChestSortBusy);
        }

        driver.Release(id);
        chest = null;
    }

    private static void Sort(Container target)
    {
        Inventory inventory = target.GetInventory();
        List<ItemDrop.ItemData>? items = inventory == null ? null : inventory.GetAllItems();
        if (items == null)
        {
            return;
        }

        var stacks = new List<ItemDrop.ItemData>(items);
        var entries = new List<ChestSortEntry>(stacks.Count);
        foreach (ItemDrop.ItemData item in stacks)
        {
            if (item == null || item.m_dropPrefab == null || item.m_shared == null)
            {
                Notices.Show(NoticePlacement.TopLeft, StringKeys.MessageChestSortBusy);
                return;
            }

            string prefab = item.m_dropPrefab.name;
            int rank = PluginCategoryCatalog.Current.IndexOf(PluginCategoryCatalog.Current.Resolve(prefab, ItemKindAdapter.From(item.m_shared)));
            var snapshot = new ItemStackSnapshot(prefab, item.m_stack, item.m_quality, item.m_shared.m_maxStackSize, GameItemIdentity.Of(item));
            entries.Add(new ChestSortEntry(snapshot, rank < 0 ? int.MaxValue : rank, Localization.instance.Localize(item.m_shared.m_name)));
        }

        ChestSortPlan plan = ChestSortPlanner.Plan(entries);
        int width = Math.Max(1, inventory!.GetWidth());
        for (int i = 0; i < plan.Removed.Count; i++)
        {
            items.Remove(stacks[plan.Removed[i]]);
        }

        for (int i = 0; i < plan.Slots.Count; i++)
        {
            ChestSortSlot slot = plan.Slots[i];
            ItemDrop.ItemData item = stacks[slot.Source];
            item.m_stack = slot.Count;
            item.m_gridPos = new Vector2i(i % width, i / width);
        }

        inventory.Changed();
        Notices.Show(NoticePlacement.TopLeft, StringKeys.MessageChestSorted, plan.Slots.Count);
    }
}
