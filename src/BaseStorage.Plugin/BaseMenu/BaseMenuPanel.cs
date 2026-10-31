namespace BaseStorage.Plugin.BaseMenu;

using System;
using System.Collections.Generic;
using BaseStorage.Core.Abstractions;
using BaseStorage.Core.Bases.Resolution;
using BaseStorage.Core.Menu;
using BaseStorage.Core.Organize.Run;
using BaseStorage.Core.Persistence;
using BaseStorage.Core.Settings;
using BaseStorage.Core.Snapshots;
using BaseStorage.Core.Ui;
using BaseStorage.Plugin.ChestCategories;
using BaseStorage.Plugin.Feeding;
using BaseStorage.Plugin.Organize;
using BaseStorage.Plugin.Patching;
using BaseStorage.Plugin.Ui;
using BaseStorage.Plugin.World;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The BaseStorage menu (Epic C): a sixth icon in the row under the player's name opens a list of the chests in the base the player
/// stands in. Clicking a chest opens the game's own chest window on it, so slots, moving items, splitting and tooltips are vanilla;
/// the window's close distance is lifted only while that chest is open. The open chest's row is lit, and the list scrolls to it when a
/// chest opens, however it was opened. Positions are measured from the live UI (rule R2). Rows are built once and reused. Main thread only.
/// </summary>
internal static class BaseMenuPanel
{
    private const string ClickKey = "BaseStorage.Plugin.BaseMenu.BaseMenuPanel.Click";
    private const string OpenId = "menu.open";
    private const string PanelId = "menu.panel";
    private const string ListId = "menu.list";
    private const string RowsId = "menu.rows";
    private const string RowId = "menu.row";
    private const string RuleId = "menu.row.rule";
    private const string HeaderId = "menu.header";
    private const string RowLabelId = "menu.row.label";
    private const string RuleLabelId = "menu.row.rule.label";
    private const string OpenHintId = "menu.row.open";
    private const float OpenHintWidth = 64f;
    private const string OrganizeId = "menu.organize";
    private const string DepositId = "menu.deposit";
    private const string CloseId = "menu.close";
    private const float MinWidth = 380f;
    internal const float MaxWidth = 560f;
    private const float PanelHeight = 700f;
    private const float PanelGap = 12f;
    private const float Margin = 12f;
    private const float RowHeight = 34f;
    private const float RuleWidth = 170f;
    private const string TakeId = "menu.take";
    private const string PlaceId = "menu.place";
    private const string HighlightId = "menu.highlight";
    private const string ShareId = "menu.share";
    private const float SecondRow = -92f;
    private const float ListTop = -140f;
    private const float RowTint = 0.18f;
    private const float FarEnough = 200f;
    private const float ScrollSpeed = 120f;
    private const int MaxRows = 150;
    private static readonly TimeSpan ReloadInterval = TimeSpan.FromSeconds(1);
    private static readonly Color RestTint = new Color(1f, 1f, 1f, 0.35f);

    private static readonly List<RowView> RowViews = new List<RowView>();
    private static readonly Dictionary<ContainerId, Container> Loaded = new Dictionary<ContainerId, Container>();
    private static readonly List<Container> Scratch = new List<Container>();
    private static readonly Vector3[] Corners = new Vector3[4];

    private static PluginWorld? world;
    private static IClock? clock;
    private static BaseStorageSettings? settings;
    private static Button? openButton;
    private static GameObject? panel;
    private static TMP_Text? header;
    private static RectTransform? listContent;
    private static ScrollRect? listScroll;
    private static Container? seenChest;
    private static int seenFrame;
    private static bool scrollPending;
    private static InventoryGui? builtFor;
    private static bool open;
    private static float savedCloseDistance = -1f;
    private static Button? takeButton;
    private static Button? placeButton;
    private static Button? highlightButton;
    private static Button? shareButton;
    private static TimeSpan lastLoad = TimeSpan.Zero;
    private static IReadOnlyList<BaseMenuRow> rows = Array.Empty<BaseMenuRow>();

    /// <summary>Stores the services the menu reads.</summary>
    /// <param name="current">The plugin world.</param>
    /// <param name="time">The clock.</param>
    /// <param name="config">The settings.</param>
    internal static void Initialize(PluginWorld current, IClock time, BaseStorageSettings config)
    {
        world = current;
        clock = time;
        settings = config;
    }

    /// <summary>Keeps the icon present and, while the menu is open, refreshes it. Called once per frame while the inventory is shown.</summary>
    /// <param name="gui">The inventory GUI.</param>
    internal static void Refresh(InventoryGui gui)
    {
        if (world == null || clock == null || gui == null)
        {
            return;
        }

        if (!ReferenceEquals(builtFor, gui))
        {
            builtFor = gui;
            openButton = null;
            panel = null;
            open = false;
            savedCloseDistance = -1f;
            RowViews.Clear();
        }

        FeedPanel.Refresh(gui);
        if (savedCloseDistance >= 0f && gui.m_currentContainer == null)
        {
            gui.m_autoCloseDistance = savedCloseDistance;
            savedCloseDistance = -1f;
        }

        if (openButton == null)
        {
            CreateOpenIcon(gui);
        }

        if (!open)
        {
            return;
        }

        if (panel == null)
        {
            BuildPanel(gui);
        }

        // A gap in the frames means the menu or the inventory was closed, so the same chest counts as newly opened.
        bool resumed = Time.frameCount - seenFrame > 1;
        seenFrame = Time.frameCount;
        Container? current = gui.m_currentContainer;
        if (resumed || !ReferenceEquals(current, seenChest))
        {
            seenChest = current;
            scrollPending = current != null;
            lastLoad = TimeSpan.Zero;
        }

        TimeSpan now = clock.Elapsed;
        if (now < lastLoad || now - lastLoad >= ReloadInterval)
        {
            lastLoad = now;
            Load();
        }
    }

    /// <summary>
    /// Clones the raven, so the icon has the row's own shadow, tint and hover, and swaps only its picture. Each row icon is a dark
    /// shadow image with the gold image on top, coloured by the button.
    /// </summary>
    private static void CreateOpenIcon(InventoryGui gui)
    {
        Transform info = gui.m_infoPanel;
        List<RectTransform> row = info == null ? new List<RectTransform>() : IconRow(info);
        Button? raven = row.Count == 0 ? null : row[0].GetComponent<Button>();
        if (raven == null)
        {
            return;
        }

        RectTransform first = row[0];
        Vector2 size = first.sizeDelta;
        Button? clone = VanillaButtonFactory.Create(raven, first.parent, OpenId, StringKeys.MenuButton, OnOpenClick);
        if (clone == null)
        {
            return;
        }

        var rect = (RectTransform)clone.transform;
        rect.sizeDelta = size;
        Image? picture = Picture(clone);
        if (picture != null)
        {
            picture.sprite = BaseMenuIcon.Sprite;
            picture.preserveAspect = true;
        }

        foreach (UIGamePad pad in clone.GetComponentsInChildren<UIGamePad>(true))
        {
            pad.enabled = false;
        }

        UITooltip? tooltip = clone.GetComponent<UITooltip>();
        if (tooltip != null)
        {
            tooltip.m_topic = string.Empty;
            tooltip.m_text = StringTable.English.Get(StringKeys.MenuButton);
        }

        if (first.parent.GetComponent<HorizontalOrVerticalLayoutGroup>() == null)
        {
            clone.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            Respace(row, rect);
        }

        openButton = clone;
        clone.gameObject.SetActive(true);
    }

    /// <summary>Finds the clone's picture: the image the button colours, or else the first image on top of the shadow.</summary>
    private static Image? Picture(Button clone)
    {
        Image? root = clone.GetComponent<Image>();
        if (clone.targetGraphic is Image target && target != root)
        {
            return target;
        }

        foreach (Image image in clone.GetComponentsInChildren<Image>(true))
        {
            if (image != root)
            {
                return image;
            }
        }

        return root;
    }

    /// <summary>Finds the icons of the row under the player's name, left to right, by where they really are on screen.</summary>
    private static List<RectTransform> IconRow(Transform info)
    {
        var all = new List<RectTransform>();
        foreach (Selectable icon in info.GetComponentsInChildren<Selectable>(false))
        {
            if (icon.transform is RectTransform rect)
            {
                all.Add(rect);
            }
        }

        all.Sort(static (a, b) => a.position.x.CompareTo(b.position.x));
        var row = new List<RectTransform>();
        for (int i = 0; i < all.Count; i++)
        {
            if (row.Count == 0 || Mathf.Abs(all[i].position.y - row[0].position.y) <= row[0].rect.height * row[0].lossyScale.y * 0.5f)
            {
                row.Add(all[i]);
            }
        }

        return row;
    }

    /// <summary>Spreads the row and the new icon evenly across the span the row already uses.</summary>
    private static void Respace(List<RectTransform> row, RectTransform added)
    {
        float left = row[0].position.x;
        float right = row[row.Count - 1].position.x;
        float step = row.Count > 1 ? (right - left) / (row.Count - 1) * ((row.Count - 1f) / row.Count) : added.rect.width * added.lossyScale.x * 1.2f;
        for (int i = 0; i < row.Count; i++)
        {
            Vector3 at = row[i].position;
            at.x = left + (i * step);
            row[i].position = at;
        }

        Vector3 last = row[0].position;
        last.x = left + (row.Count * step);
        added.position = last;
    }

    private static void CopyTooltip(RectTransform from, GameObject to)
    {
        UITooltip? source = from.GetComponent<UITooltip>();
        if (source != null)
        {
            UITooltip tooltip = to.AddComponent<UITooltip>();
            tooltip.m_tooltipPrefab = source.m_tooltipPrefab;
            tooltip.m_text = StringTable.English.Get(StringKeys.MenuButton);
        }
    }

    private static void OnOpenClick() => PatchSafety.Run(ClickKey, static () => Toggle());

    /// <summary>Closes the menu, because another panel needs its place.</summary>
    internal static void Hide()
    {
        if (open)
        {
            Toggle();
        }
    }

    private static void Toggle()
    {
        FeedPanel.Close();
        open = !open;
        lastLoad = TimeSpan.Zero;
        CategoryDropdown.Close();
        if (panel != null)
        {
            panel.SetActive(open);
            if (open && builtFor != null)
            {
                Fit(builtFor);
            }
        }
    }

    private static void BuildPanel(InventoryGui gui)
    {
        panel = VanillaButtonFactory.CreateNode(gui.m_inventoryRoot, PanelId, new Vector2(MaxWidth, PanelHeight), Vector2.zero, typeof(RectTransform), typeof(Image));
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

        header = Text(gui, panel.transform, HeaderId, Margin, -Margin, MaxWidth - (2 * Margin), 30f);
        Transform parent = panel.transform;
        // Not cloned from Craft: a clone keeps the colours its template had at that moment as its normal look, and Craft is often greyed out.
        Place(VanillaButtonFactory.Create(gui.m_stackAllButton, parent, OrganizeId, StringKeys.StationActionsOrganizeButton, OnOrganizeClick), Margin, 170f);
        Place(VanillaButtonFactory.Create(gui.m_stackAllButton, parent, DepositId, StringKeys.StationActionsDepositButton, OnDepositClick), Margin + 176f, 200f);
        Place(VanillaButtonFactory.Create(gui.m_stackAllButton, parent, CloseId, StringKeys.MenuClose, OnOpenClick), Margin + 382f, 130f);
        takeButton = VanillaButtonFactory.Create(gui.m_takeAllButton, parent, TakeId, StringKeys.MenuTakeAll, OnTakeClick);
        Place(takeButton, Margin, 130f, SecondRow);
        placeButton = VanillaButtonFactory.Create(gui.m_stackAllButton, parent, PlaceId, StringKeys.MenuPlaceStacks, OnPlaceClick);
        Place(placeButton, Margin + 136f, 160f, SecondRow);
        highlightButton = VanillaButtonFactory.Create(gui.m_stackAllButton, parent, HighlightId, StringKeys.MenuHighlight, OnHighlightClick);
        Place(highlightButton, Margin + 302f, 120f, SecondRow);
        shareButton = VanillaButtonFactory.Create(gui.m_stackAllButton, parent, ShareId, StringKeys.MenuShare, OnShareClick);
        Place(shareButton, Margin + 428f, 108f, SecondRow);
        CreateList(gui);
        Fit(gui);
    }

    private static void Place(Button? button, float x, float width, float y = -48f)
    {
        if (button != null)
        {
            VanillaButtonFactory.Pin((RectTransform)button.transform, new Vector2(width, 40f), new Vector2(x, y));
            button.interactable = true;
            button.gameObject.SetActive(true);
        }
    }

    private static void Fit(InventoryGui gui) => FitBetween(gui, (RectTransform)panel!.transform, PanelHeight);

    /// <summary>Sits a panel between the chest window and the crafting panel, measured from both.</summary>
    /// <param name="gui">The inventory GUI.</param>
    /// <param name="rect">The panel.</param>
    /// <param name="height">The panel's height.</param>
    internal static void FitBetween(InventoryGui gui, RectTransform rect, float height)
    {
        var parent = (RectTransform)rect.parent;
        float left = LocalEdge(gui.m_container, parent, right: true) + PanelGap;
        float right = LocalEdge(gui.m_crafting, parent, right: false) - PanelGap;
        float width = Mathf.Clamp(right - left, MinWidth, MaxWidth);
        rect.sizeDelta = new Vector2(width, height);
        Rect bounds = parent.rect;
        rect.anchoredPosition = new Vector2(left - bounds.xMin, -PanelGap);
    }

    private static float LocalEdge(RectTransform target, RectTransform parent, bool right)
    {
        target.GetWorldCorners(Corners);
        return parent.InverseTransformPoint(Corners[right ? 2 : 0]).x;
    }

    private static void CreateList(InventoryGui gui)
    {
        float width = MaxWidth - (2 * Margin);
        GameObject view = VanillaButtonFactory.CreateNode(panel!.transform, ListId, new Vector2(width, PanelHeight + ListTop - Margin), new Vector2(Margin, ListTop), typeof(RectTransform), typeof(RectMask2D), typeof(ScrollRect), typeof(Image));
        var viewRect = (RectTransform)view.transform;
        view.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.35f);
        listContent = (RectTransform)VanillaButtonFactory.CreateNode(view.transform, RowsId, new Vector2(width, 0f), Vector2.zero, typeof(RectTransform)).transform;
        ScrollRect scroll = view.GetComponent<ScrollRect>();
        listScroll = scroll;
        scroll.content = listContent;
        scroll.viewport = viewRect;
        scroll.horizontal = false;
        scroll.scrollSensitivity = ScrollSpeed;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        for (int i = 0; i < MaxRows; i++)
        {
            RowViews.Add(new RowView(gui, listContent, i, width));
        }
    }

    private static TMP_Text? Text(InventoryGui gui, Transform parent, string id, float x, float y, float width, float height)
    {
        TMP_Text? text = VanillaButtonFactory.CreateText(gui.m_craftingStationName, parent, id);
        if (text != null)
        {
            VanillaButtonFactory.Pin((RectTransform)text.transform, new Vector2(width, height), new Vector2(x, y));
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.enableAutoSizing = false;
            text.fontSize = 18f;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.raycastTarget = false;
            text.gameObject.SetActive(true);
        }

        return text;
    }

    private static void Load()
    {
        PluginWorld? current = world;
        BaseStorageSettings? config = settings;
        Player player = Player.m_localPlayer;
        if (current == null || config == null || player == null)
        {
            return;
        }

        Vector3 at = player.transform.position;
        var position = new WorldPosition(at.x, at.y, at.z);
        BaseResolution resolution = current.Cache.Resolve(BaseStart.FromPosition(position), new BaseCacheOptions(config.BaseCacheSeconds, config.MaxBaseStations));
        if (!resolution.IsInBase)
        {
            rows = Array.Empty<BaseMenuRow>();
            SetText(header, StringTable.English.Get(StringKeys.MessageBaseNotInBase));
            Show();
            return;
        }

        LoadContainers(current);
        var assignments = new Dictionary<ContainerId, CategoryAssignment>();
        for (int i = 0; i < resolution.Containers.Count; i++)
        {
            ContainerId id = resolution.Containers[i].Id;
            if (Loaded.TryGetValue(id, out Container? chest))
            {
                CategoryAssignment? assignment = ChestCategoryWriter.Read(chest);
                if (assignment != null)
                {
                    assignments[id] = assignment;
                }
            }
        }

        rows = BaseMenuBuilder.Rows(resolution.Containers, assignments, position, null, StringTable.English);
        SetText(header, StringTable.English.Format(StringKeys.MenuHeader, resolution.Stations.Count, resolution.Containers.Count, BaseMenuBuilder.FillPercent(resolution.Containers)));
        Show();
    }

    private static void LoadContainers(PluginWorld current)
    {
        Loaded.Clear();
        Scratch.Clear();
        current.Containers.CopyItemsTo(Scratch);
        for (int i = 0; i < Scratch.Count; i++)
        {
            Container chest = Scratch[i];
            ZDO? zdo = chest != null && chest.m_nview != null && chest.m_nview.IsValid() ? chest.m_nview.GetZDO() : null;
            if (zdo != null)
            {
                Loaded[new ContainerId(zdo.m_uid.UserID, zdo.m_uid.ID)] = chest!;
            }
        }
    }

    private static void Show()
    {
        Container? current = builtFor == null ? null : builtFor.m_currentContainer;
        SetEnabled(takeButton, current != null);
        SetEnabled(placeButton, current != null);
        SetEnabled(highlightButton, current != null);
        SetEnabled(shareButton, current != null);
        int openRow = -1;
        for (int i = 0; i < RowViews.Count; i++)
        {
            BaseMenuRow? row = i < rows.Count ? rows[i] : null;
            bool isOpen = row != null && current != null && Loaded.TryGetValue(row.Chest.Id, out Container? chest) && ReferenceEquals(chest, current);
            RowViews[i].Show(row, isOpen);
            if (isOpen)
            {
                openRow = i;
            }
        }

        if (listContent != null)
        {
            listContent.sizeDelta = new Vector2(listContent.sizeDelta.x, Math.Min(rows.Count, MaxRows) * RowHeight);
            if (scrollPending && openRow >= 0)
            {
                ScrollTo(openRow);
            }
        }
    }

    /// <summary>Scrolls once per opened chest, so a player can still scroll away from it. Rows count down from the top of the list.</summary>
    private static void ScrollTo(int row)
    {
        scrollPending = false;
        if (listContent != null && listScroll != null)
        {
            Vector2 position = listContent.anchoredPosition;
            position.y = BaseMenuBuilder.ScrollOffset(row, Math.Min(rows.Count, MaxRows), RowHeight, listScroll.viewport.rect.height, position.y);
            listScroll.StopMovement();
            listContent.anchoredPosition = position;
        }
    }

    private static void SetEnabled(Button? button, bool enabled)
    {
        if (button != null && button.interactable != enabled)
        {
            button.interactable = enabled;
        }
    }

    private static void SetText(TMP_Text? text, string value)
    {
        if (text != null && !string.Equals(text.text, value, StringComparison.Ordinal))
        {
            text.text = value;
        }
    }

    /// <summary>Opens a chest in the game's own window, through its normal request, so access and in-use rules are vanilla.</summary>
    private static void OpenChest(int index)
    {
        InventoryGui? gui = builtFor;
        Player player = Player.m_localPlayer;
        if (gui == null || player == null || index >= rows.Count || rows[index].AccessDenied || !Loaded.TryGetValue(rows[index].Chest.Id, out Container? chest))
        {
            return;
        }

        CategoryDropdown.Close();
        lastLoad = TimeSpan.Zero;
        if (ReferenceEquals(gui.m_currentContainer, chest))
        {
            gui.CloseContainer();
            return;
        }

        if (savedCloseDistance < 0f)
        {
            savedCloseDistance = gui.m_autoCloseDistance;
        }

        gui.CloseContainer();
        gui.m_autoCloseDistance = FarEnough;
        chest.Interact(player, false, false);
        lastLoad = TimeSpan.Zero;
    }

    private static void OpenRules(int index, RectTransform anchor)
    {
        InventoryGui? gui = builtFor;
        if (gui != null && index < rows.Count && Loaded.TryGetValue(rows[index].Chest.Id, out Container? chest))
        {
            CategoryDropdown.Toggle(gui, chest, anchor);
            lastLoad = TimeSpan.Zero;
        }
    }

    private static void OnTakeClick() => PatchSafety.Run(ClickKey, static () => builtFor?.OnTakeAll());

    private static void OnPlaceClick() => PatchSafety.Run(ClickKey, static () => builtFor?.OnStackAll());

    private static void OnHighlightClick() => PatchSafety.Run(ClickKey, static () =>
    {
        Container? current = builtFor == null ? null : builtFor.m_currentContainer;
        if (current != null)
        {
            ChestHighlight.Toggle(current);
        }
    });

    private static void OnShareClick() => PatchSafety.Run(ClickKey, static () =>
    {
        Container? current = builtFor == null ? null : builtFor.m_currentContainer;
        if (current != null)
        {
            ChestHighlight.Share(current);
        }
    });

    private static void OnOrganizeClick() => PatchSafety.Run(ClickKey, static () => Start(deposit: false));

    private static void OnDepositClick() => PatchSafety.Run(ClickKey, static () => Start(deposit: true));

    private static void Start(bool deposit)
    {
        Player player = Player.m_localPlayer;
        if (player == null)
        {
            return;
        }

        Vector3 at = player.transform.position;
        BaseStart start = BaseStart.FromPosition(new WorldPosition(at.x, at.y, at.z));
        OrganizeStartOutcome _ = deposit ? OrganizeRunner.StartDepositAt(start) : OrganizeRunner.StartAt(start);
        lastLoad = TimeSpan.Zero;
    }

    private sealed class RowView
    {
        private readonly GameObject root;
        private readonly Image background;
        private readonly Button click;
        private readonly TMP_Text? label;
        private readonly TMP_Text? rule;
        private readonly TMP_Text? openHint;
        private bool lit;

        internal RowView(InventoryGui gui, Transform parent, int index, float width)
        {
            float labelWidth = width - RuleWidth - 2f;
            root = VanillaButtonFactory.CreateNode(parent, RowId, new Vector2(labelWidth, RowHeight - 2f), new Vector2(0f, -index * RowHeight), typeof(RectTransform), typeof(Image), typeof(Button));
            background = root.GetComponent<Image>();
            click = root.GetComponent<Button>();
            click.targetGraphic = background;
            ColorBlock colors = click.colors;
            colors.normalColor = RestTint;
            colors.highlightedColor = new Color(1f, 0.8f, 0.3f, 1f);
            colors.pressedColor = new Color(1f, 0.7f, 0.2f, 1f);
            colors.selectedColor = colors.normalColor;
            click.colors = colors;
            click.onClick.AddListener(() => PatchSafety.Run(ClickKey, () => OpenChest(index)));
            label = Text(gui, root.transform, RowLabelId, 6f, 0f, labelWidth - OpenHintWidth - 10f, RowHeight - 2f);
            openHint = Text(gui, root.transform, OpenHintId, labelWidth - OpenHintWidth, 0f, OpenHintWidth - 4f, RowHeight - 2f);
            if (openHint != null)
            {
                openHint.alignment = TextAlignmentOptions.MidlineRight;
                openHint.alpha = 0.6f;
            }

            RectTransform? cycleRect = null;
            GameObject cycle = Cell(root.transform, RuleId, RuleWidth, width - RuleWidth, () => OpenRules(index, cycleRect!));
            cycleRect = (RectTransform)cycle.transform;
            rule = Text(gui, cycle.transform, RuleLabelId, 6f, 0f, RuleWidth - 10f, RowHeight - 2f);
            root.SetActive(false);
        }

        private static GameObject Cell(Transform parent, string id, float width, float x, Action click)
        {
            GameObject cell = VanillaButtonFactory.CreateNode(parent, id, new Vector2(width, RowHeight - 2f), new Vector2(x, 0f), typeof(RectTransform), typeof(Image), typeof(Button));
            cell.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.45f);
            cell.GetComponent<Button>().onClick.AddListener(() => PatchSafety.Run(ClickKey, click));
            return cell;
        }

        internal void Show(BaseMenuRow? row, bool isOpen)
        {
            if (row == null)
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

            // The button multiplies its tint into the row colour, so the resting tint would fade the open row to almost nothing.
            if (lit != isOpen)
            {
                lit = isOpen;
                ColorBlock colors = click.colors;
                colors.normalColor = isOpen ? Color.white : RestTint;
                colors.selectedColor = colors.normalColor;
                click.colors = colors;
            }

            background.color = isOpen ? new Color(1f, 0.8f, 0.3f, 0.6f) : new Color(1f, 1f, 1f, RowTint);
            SetText(openHint, StringTable.English.Get(isOpen ? StringKeys.MenuClose : StringKeys.MenuOpen));

            string state = row.AccessDenied ? StringTable.English.Get(StringKeys.MenuNoAccess) : row.InUse ? StringTable.English.Get(StringKeys.MenuInUse) : string.Empty;
            SetText(label, StringTable.English.Format(StringKeys.MenuRow, row.FillPercent, (int)Mathf.Sqrt(row.DistanceSquared), state));
            SetText(rule, row.RuleLabel);
        }
    }
}
