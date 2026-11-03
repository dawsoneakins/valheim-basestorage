namespace BaseStorage.Plugin.Feeding;

using System;
using System.Collections.Generic;
using BaseStorage.Core.Abstractions;
using BaseStorage.Core.Feeding;
using BaseStorage.Core.Snapshots;
using BaseStorage.Core.Ui;
using BaseStorage.Plugin.BaseMenu;
using BaseStorage.Plugin.Patching;
using BaseStorage.Plugin.Ui;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The panel of one station: every item it accepts, how many the base holds, whether the station is kept fed with it, and how many must
/// stay in the base. Shift + use on the station, or on its ore or fuel switch, opens it inside the inventory screen, which gives it a cursor and
/// closes it with the usual keys. Rows are built once and reused. Main thread only.
/// </summary>
internal static class FeedPanel
{
    private const string ClickKey = "BaseStorage.Plugin.Feeding.FeedPanel.Click";
    private const string PanelId = "feed.panel";
    private const string TitleId = "feed.title";
    private const string StatusId = "feed.status";
    private const string CloseId = "feed.close";
    private const string HeaderItemId = "feed.header.item";
    private const string HeaderFedId = "feed.header.fed";
    private const string HeaderKeepId = "feed.header.keep";
    private const string RowId = "feed.row";
    private const string IconId = "feed.row.icon";
    private const string NameId = "feed.row.name";
    private const string StockId = "feed.row.stock";
    private const string ToggleId = "feed.row.toggle";
    private const string LessId = "feed.row.less";
    private const string KeepId = "feed.row.keep";
    private const string MoreId = "feed.row.more";
    private const float PanelHeight = 700f;
    private const float Margin = 12f;
    private const float RowsTop = -112f;
    private const float RowHeight = 42f;
    private const float IconSize = 34f;
    private const int MaxRows = 13;
    private const int KeepStep = 10;
    private static readonly TimeSpan ReloadInterval = TimeSpan.FromSeconds(1);

    // A change reaches another player's station a moment later, so the panel trusts its own value for this long.
    private static readonly TimeSpan EchoDelay = TimeSpan.FromSeconds(2);
    private static readonly Color OnColor = new Color(1f, 0.8f, 0.3f, 1f);

    private static readonly List<RowView> RowViews = new List<RowView>();
    private static readonly List<RowItem> Items = new List<RowItem>();
    private static readonly List<ContainerSnapshot> Chests = new List<ContainerSnapshot>();
    private static readonly List<ItemDrop> Accepted = new List<ItemDrop>();

    private static IClock? clock;
    private static InventoryGui? builtFor;
    private static GameObject? panel;
    private static TMP_Text? title;
    private static TMP_Text? status;
    private static FeedTarget? station;
    private static FeedSelection selection = FeedSelection.Empty;
    private static TimeSpan lastLoad = TimeSpan.Zero;
    private static TimeSpan lastEdit = TimeSpan.Zero;

    /// <summary>Stores the clock.</summary>
    /// <param name="time">The clock.</param>
    internal static void Initialize(IClock time) => clock = time;

    /// <summary>Opens the panel from Shift + use on a station's switch.</summary>
    /// <param name="used">The switch.</param>
    /// <param name="alt">True when Shift was held.</param>
    /// <returns>True to run vanilla; false when the panel took the interaction.</returns>
    internal static bool OnSwitchInteract(Switch used, bool alt)
    {
        if (!alt)
        {
            return true;
        }

        FeedTarget? target = StationOf(used);
        if (target == null)
        {
            return true;
        }

        Open(target);
        return false;
    }

    /// <summary>Opens the panel from Shift + use on a station with no switch of its own.</summary>
    /// <param name="used">The station.</param>
    /// <param name="hold">True when the use key is being held down.</param>
    /// <param name="alt">True when Shift was held.</param>
    /// <returns>True to run vanilla; false when the panel took the interaction.</returns>
    internal static bool OnStationInteract(Component used, bool hold, bool alt)
    {
        FeedTarget? target = !alt || hold ? null : FeedTargets.Of(used);
        if (target == null)
        {
            return true;
        }

        Open(target);
        return false;
    }

    /// <summary>Adds the panel's hint to a station switch's hover text.</summary>
    /// <param name="hovered">The switch.</param>
    /// <param name="text">The vanilla text.</param>
    /// <returns>The text to show.</returns>
    internal static string HoverText(Switch hovered, string text)
    {
        return WithHint(StationOf(hovered), text);
    }

    /// <summary>Adds the panel's hint to a station's hover text.</summary>
    /// <param name="hovered">The station.</param>
    /// <param name="text">The vanilla text.</param>
    /// <returns>The text to show.</returns>
    internal static string HoverText(Component hovered, string text) => WithHint(FeedTargets.Of(hovered), text);

    /// <summary>Gets a value indicating whether the panel is open.</summary>
    internal static bool IsOpen => station != null && panel != null && panel.activeSelf;

    /// <summary>Lists the panel's controls for the gamepad, row by row.</summary>
    /// <param name="add">Receives each control and what pressing it does.</param>
    /// <returns>The controls in one row.</returns>
    internal static int CopyTargets(Action<Selectable?, Action> add)
    {
        for (int i = 0; i < Items.Count && i < RowViews.Count; i++)
        {
            int index = i;
            RowViews[i].CopyTargets(add, () => Toggle(index), () => ChangeKeep(index, -KeepStep), () => ChangeKeep(index, KeepStep));
        }

        return 3;
    }

    /// <summary>Closes the panel.</summary>
    internal static void Close()
    {
        station = null;
        if (panel != null)
        {
            panel.SetActive(false);
        }
    }

    /// <summary>Closes the panel when the inventory screen has closed. Called once per frame.</summary>
    internal static void Tick()
    {
        if (station != null && !InventoryGui.IsVisible())
        {
            Close();
        }
    }

    /// <summary>Refreshes the open panel. Called once per frame while the inventory is shown.</summary>
    /// <param name="gui">The inventory GUI.</param>
    internal static void Refresh(InventoryGui gui)
    {
        if (!ReferenceEquals(builtFor, gui))
        {
            builtFor = gui;
            panel = null;
            RowViews.Clear();
        }

        if (clock == null || panel == null || !panel.activeSelf)
        {
            return;
        }

        if (station == null || !station.IsAlive)
        {
            Close();
            return;
        }

        TimeSpan now = clock.Elapsed;
        if (now < lastLoad || now - lastLoad >= ReloadInterval)
        {
            lastLoad = now;
            if (now - lastEdit >= EchoDelay)
            {
                selection = StationFeeder.Read(station);
            }

            Show();
        }
    }

    private static string WithHint(FeedTarget? target, string text)
    {
        if (target == null || Localization.instance == null)
        {
            return text;
        }

        string hint = text + "\n" + Localization.instance.Localize("[<color=yellow><b>$KEY_AltPlace + $KEY_Use</b></color>] ")
            + StringTable.English.Get(StringKeys.FeedHover);
        return StationFeeder.Read(target).Entries.Count == 0 ? hint : hint + "\n" + StringTable.English.Get(StringKeys.FeedKept);
    }

    /// <summary>Finds the station of a switch: only its input and fuel switches count, not the one that empties it.</summary>
    private static FeedTarget? StationOf(Switch used)
    {
        Smelter? smelter = used == null ? null : used.GetComponentInParent<Smelter>();
        if (smelter != null && (smelter.m_addOreSwitch == used || smelter.m_addWoodSwitch == used))
        {
            return FeedTargets.Of(smelter);
        }

        CookingStation? cooker = used == null ? null : used.GetComponentInParent<CookingStation>();
        return cooker != null && (cooker.m_addFoodSwitch == used || cooker.m_addFuelSwitch == used) ? FeedTargets.Of(cooker) : null;
    }

    private static void Open(FeedTarget target)
    {
        InventoryGui? gui = InventoryGui.instance;
        if (gui == null || clock == null)
        {
            return;
        }

        gui.Show(null);
        BaseMenuPanel.Hide();
        station = target;
        selection = StationFeeder.Read(target);
        if (!ReferenceEquals(builtFor, gui) || panel == null)
        {
            builtFor = gui;
            RowViews.Clear();
            Build(gui);
        }

        Items.Clear();
        Accepted.Clear();
        target.CopyItems(Accepted);
        for (int i = 0; i < Accepted.Count && Items.Count < MaxRows; i++)
        {
            if (!Listed(Accepted[i].gameObject.name))
            {
                Items.Add(new RowItem(Accepted[i]));
            }
        }

        panel!.SetActive(true);
        BaseMenuPanel.FitBetween(gui, (RectTransform)panel.transform, PanelHeight);
        SetText(title, Localization.instance.Localize(target.Name));
        Show();
        lastLoad = clock.Elapsed;
    }

    private static bool Listed(string prefab)
    {
        for (int i = 0; i < Items.Count; i++)
        {
            if (string.Equals(Items[i].Prefab, prefab, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static void Build(InventoryGui gui)
    {
        panel = VanillaButtonFactory.CreateNode(gui.m_inventoryRoot, PanelId, new Vector2(BaseMenuPanel.MaxWidth, PanelHeight), Vector2.zero, typeof(RectTransform), typeof(Image));
        Image background = panel.GetComponent<Image>();
        Image? vanilla = gui.m_crafting.GetComponent<Image>();
        if (vanilla != null && vanilla.sprite != null)
        {
            background.sprite = vanilla.sprite;
            background.type = vanilla.type;
            background.color = vanilla.color;
        }
        else
        {
            background.color = new Color(0.3f, 0.24f, 0.25f, 1f);
        }

        Transform parent = panel.transform;
        title = Label(gui.m_craftingStationName, parent, TitleId, Margin, -Margin, 380f, 30f, 22f);
        status = Label(gui.m_recipeDecription, parent, StatusId, Margin, -46f, 380f, 26f, 17f);
        SetText(Label(gui.m_recipeDecription, parent, HeaderItemId, Margin, -82f, 300f, 24f, 15f), StringTable.English.Get(StringKeys.FeedHeaderItem));
        SetText(Label(gui.m_recipeDecription, parent, HeaderFedId, RowView.ToggleX, -82f, RowView.ToggleWidth, 24f, 15f), StringTable.English.Get(StringKeys.FeedHeaderFed));
        SetText(Label(gui.m_recipeDecription, parent, HeaderKeepId, RowView.LessX, -82f, 150f, 24f, 15f), StringTable.English.Get(StringKeys.FeedHeaderKeep));
        Button? close = VanillaButtonFactory.Create(gui.m_stackAllButton, parent, CloseId, StringKeys.MenuClose, OnCloseClick);
        if (close != null)
        {
            Pin(close, 130f, 40f, BaseMenuPanel.MaxWidth - Margin - 130f - 12f, -Margin);
        }

        for (int i = 0; i < MaxRows; i++)
        {
            RowViews.Add(new RowView(gui, parent, i));
        }
    }

    private static void Show()
    {
        FeedTarget? target = station;
        if (target == null || !target.IsAlive)
        {
            return;
        }

        FeedStationState state = StationFeeder.StateOf(target);
        SetText(status, state.Shape == FeedStationShape.FuelOnly
            ? StringTable.English.Format(StringKeys.FeedStatusFuelOnly, Mathf.CeilToInt(state.Fuel), Mathf.FloorToInt(state.MaxFuel))
            : state.FuelPrefab == null
            ? StringTable.English.Format(StringKeys.FeedStatusNoFuel, state.Queued, state.MaxQueued)
            : StringTable.English.Format(StringKeys.FeedStatus, state.Queued, state.MaxQueued, Mathf.CeilToInt(state.Fuel), Mathf.FloorToInt(state.MaxFuel)));
        bool inBase = StationFeeder.LoadChests(target.Station.transform.position, Chests);
        for (int i = 0; i < RowViews.Count; i++)
        {
            RowItem? item = i < Items.Count ? Items[i] : null;
            FeedEntry? entry = item == null ? null : selection.Find(item.Prefab);
            RowViews[i].Show(item, entry, item == null || !inBase ? 0 : FeedPlanner.CountInBase(Chests, item.Prefab));
        }

        if (!inBase)
        {
            SetText(status, StringTable.English.Get(StringKeys.MessageBaseNotInBase));
        }
    }

    private static void OnCloseClick() => PatchSafety.Run(ClickKey, static () =>
    {
        InventoryGui? gui = InventoryGui.instance;
        Close();
        if (gui != null)
        {
            gui.Hide();
        }
    });

    private static void Toggle(int index)
    {
        if (index < Items.Count)
        {
            RowItem item = Items[index];
            Store(selection.Find(item.Prefab) == null ? selection.With(item.Prefab, item.Keep) : selection.Without(item.Prefab));
        }
    }

    private static void ChangeKeep(int index, int change)
    {
        if (index < Items.Count)
        {
            RowItem item = Items[index];
            FeedEntry? entry = selection.Find(item.Prefab);
            item.Keep = Math.Max(0, (entry == null ? item.Keep : entry.Keep) + change);
            Store(entry == null ? selection : selection.With(item.Prefab, item.Keep));
        }
    }

    private static void Store(FeedSelection changed)
    {
        FeedTarget? target = station;
        if (target == null || clock == null)
        {
            return;
        }

        if (!ReferenceEquals(changed, selection))
        {
            selection = changed;
            lastEdit = clock.Elapsed;
            StationFeeder.Write(target, changed);
        }

        Show();
    }

    private static TMP_Text? Label(TMP_Text template, Transform parent, string id, float x, float y, float width, float height, float size)
    {
        TMP_Text? text = VanillaButtonFactory.CreateText(template, parent, id);
        if (text != null)
        {
            // The description text sizes itself to its content, which would undo the pin below.
            ContentSizeFitter? fitter = text.GetComponent<ContentSizeFitter>();
            if (fitter != null)
            {
                fitter.enabled = false;
            }

            VanillaButtonFactory.Pin((RectTransform)text.transform, new Vector2(width, height), new Vector2(x, y));
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.enableAutoSizing = false;
            text.fontSize = size;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.raycastTarget = false;
            text.gameObject.SetActive(true);
        }

        return text;
    }

    private static void Pin(Button button, float width, float height, float x, float y)
    {
        VanillaButtonFactory.Pin((RectTransform)button.transform, new Vector2(width, height), new Vector2(x, y));
        button.interactable = true;
        button.gameObject.SetActive(true);
    }

    private static void SetText(TMP_Text? text, string value)
    {
        if (text != null && !string.Equals(text.text, value, StringComparison.Ordinal))
        {
            text.text = value;
        }
    }

    private sealed class RowItem
    {
        internal RowItem(ItemDrop item)
        {
            Prefab = item.gameObject.name;
            Name = Localization.instance.Localize(item.m_itemData.m_shared.m_name);
            Icon = item.m_itemData.GetIcon();
        }

        internal string Prefab { get; }

        internal string Name { get; }

        internal Sprite Icon { get; }

        internal int Keep { get; set; }
    }

    private sealed class RowView
    {
        internal const float ToggleX = 306f;
        internal const float ToggleWidth = 76f;
        internal const float LessX = 392f;
        private const float StepWidth = 36f;
        private const float KeepWidth = 62f;

        private readonly GameObject root;
        private readonly Image icon;
        private readonly TMP_Text? name;
        private readonly TMP_Text? stock;
        private readonly TMP_Text? keep;
        private readonly Button? toggle;
        private readonly Button? less;
        private readonly Button? more;
        private readonly TMP_Text? toggleLabel;

        internal RowView(InventoryGui gui, Transform parent, int index)
        {
            float width = BaseMenuPanel.MaxWidth - (2 * Margin);
            root = VanillaButtonFactory.CreateNode(parent, RowId + "." + index, new Vector2(width, RowHeight - 4f), new Vector2(Margin, RowsTop - (index * RowHeight)), typeof(RectTransform), typeof(Image));
            root.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.3f);
            Transform row = root.transform;
            icon = VanillaButtonFactory.CreateNode(row, IconId, new Vector2(IconSize, IconSize), new Vector2(3f, -2f), typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            name = Label(gui.m_recipeDecription, row, NameId, 44f, 0f, 170f, RowHeight - 4f, 16f);
            stock = Label(gui.m_recipeDecription, row, StockId, 216f, 0f, 86f, RowHeight - 4f, 15f);
            keep = Label(gui.m_recipeDecription, row, KeepId, LessX + StepWidth + 2f, 0f, KeepWidth - 4f, RowHeight - 4f, 16f);
            if (keep != null)
            {
                keep.alignment = TextAlignmentOptions.Center;
            }

            toggle = VanillaButtonFactory.Create(gui.m_stackAllButton, row, ToggleId, StringKeys.FeedOff, () => PatchSafety.Run(ClickKey, () => Toggle(index)));
            if (toggle != null)
            {
                Pin(toggle, ToggleWidth, 34f, ToggleX, -2f);
                toggleLabel = toggle.GetComponentInChildren<TMP_Text>();
            }

            less = VanillaButtonFactory.Create(gui.m_stackAllButton, row, LessId, StringKeys.FeedLess, () => PatchSafety.Run(ClickKey, () => ChangeKeep(index, -KeepStep)));
            if (less != null)
            {
                Pin(less, StepWidth, 34f, LessX, -2f);
            }

            more = VanillaButtonFactory.Create(gui.m_stackAllButton, row, MoreId, StringKeys.FeedMore, () => PatchSafety.Run(ClickKey, () => ChangeKeep(index, KeepStep)));
            if (more != null)
            {
                Pin(more, StepWidth, 34f, LessX + StepWidth + KeepWidth, -2f);
            }

            root.SetActive(false);
        }

        internal void CopyTargets(Action<Selectable?, Action> add, Action onToggle, Action onLess, Action onMore)
        {
            add(toggle, onToggle);
            add(less, onLess);
            add(more, onMore);
        }

        internal void Show(RowItem? item, FeedEntry? entry, int inBase)
        {
            if (item == null)
            {
                if (root.activeSelf)
                {
                    root.SetActive(false);
                }

                return;
            }

            if (!root.activeSelf)
            {
                root.SetActive(true);
            }

            icon.sprite = item.Icon;
            SetText(name, item.Name);
            SetText(stock, StringTable.English.Format(StringKeys.FeedInBase, inBase));
            SetText(keep, (entry == null ? item.Keep : entry.Keep).ToString(System.Globalization.CultureInfo.InvariantCulture));
            SetText(toggleLabel, StringTable.English.Get(entry == null ? StringKeys.FeedOff : StringKeys.FeedOn));
            if (toggleLabel != null)
            {
                toggleLabel.fontStyle = entry == null ? FontStyles.Normal : FontStyles.Bold;
            }

            if (toggle != null)
            {
                ColorBlock colors = toggle.colors;
                Color wanted = entry == null ? Color.white : OnColor;
                if (colors.normalColor != wanted)
                {
                    colors.normalColor = wanted;
                    colors.selectedColor = wanted;
                    toggle.colors = colors;
                }
            }
        }
    }
}
