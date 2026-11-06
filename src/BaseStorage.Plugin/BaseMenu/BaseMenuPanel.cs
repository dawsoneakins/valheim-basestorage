namespace BaseStorage.Plugin.BaseMenu;

using System;
using System.Collections.Generic;
using BaseStorage.Core.Abstractions;
using BaseStorage.Core.Bases.Resolution;
using BaseStorage.Core.Categories;
using BaseStorage.Core.Menu;
using BaseStorage.Core.Organize.Run;
using BaseStorage.Core.Persistence;
using BaseStorage.Core.Settings;
using BaseStorage.Core.Snapshots;
using BaseStorage.Core.Ui;
using BaseStorage.Plugin.Categories;
using BaseStorage.Plugin.CategoryButton;
using BaseStorage.Plugin.ChestCategories;
using BaseStorage.Plugin.ChestNames;
using BaseStorage.Plugin.Feeding;
using BaseStorage.Plugin.Organize;
using BaseStorage.Plugin.Favourites;
using BaseStorage.Plugin.Patching;
using BaseStorage.Plugin.Ui;
using BaseStorage.Plugin.World;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The BaseStorage menu (Epic C): a sixth icon in the row under the player's name opens a list of the chests in the base the player
/// stands in. Clicking a chest opens the game's own chest window on it, so slots, moving items, splitting and tooltips are vanilla;
/// the window's close distance is lifted only while that chest is open. A search narrows the list to the chests holding an item. The open chest's row is lit, and the list scrolls to it when a
/// chest opens, however it was opened. Positions are measured from the live UI (rule R2). Rows are built once and reused. Main thread only.
/// </summary>
internal static class BaseMenuPanel
{
    private const string ClickKey = "BaseStorage.Plugin.BaseMenu.BaseMenuPanel.Click";
    private static readonly Color IconRestColour = new Color(0.85f, 0.85f, 0.85f, 1f);
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
    private const string RestockId = "menu.restock";
    private const string AllBasesId = "menu.all-bases";
    private const string HeadingBaseId = "menu.heading.base";
    private const string HeadingChestId = "menu.heading.chest";
    private const string RowMoreId = "menu.row.more";
    private const string RowFillId = "menu.row.fill";
    private const string RowPreviewId = "menu.row.preview";
    private const float PreviewSpace = 28f;
    private const float PreviewMoreWidth = 40f;
    private const float NamedPreviewStart = 130f;
    private const float FillBarHeight = 3f;
    private const float CloseWidth = 90f;
    private const float CloseHeight = 32f;
    private const string PutAwaySelectedId = "menu.put-away-selected";
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
    private const string MarkId = "menu.mark";
    private const string SearchId = "menu.search";
    private const string SearchTextId = "menu.search.text";
    private const string SearchHintId = "menu.search.hint";
    private const string SearchClearId = "menu.search.clear";
    private const string SearchClearLabelId = "menu.search.clear.label";
    private const string SearchEmptyId = "menu.search.empty";
    private const string RowIconId = "menu.row.icon";
    private const string MatchOpen = "<color=#FFCC4D>";
    private const string MatchClose = "</color>";
    private const string SearchIconId = "menu.search.icon";
    private const string SearchAreaId = "menu.search.area";
    private const float SearchClearWidth = 64f;
    private const float SearchIconSize = 20f;
    private const float SearchPadding = 10f;
    private const float RowIconSize = 26f;
    private const float RowIconSpace = 34f;
    private const float SearchHeight = 40f;
    private const float ButtonHeight = 40f;
    private const int TakeIndex = 6;
    private const int CloseIndex = 10;

    /// <summary>Base management acts on every base, so it sits in the top bar beside Close rather than in a group.</summary>
    private const int ManagementIndex = 9;
    private const int PutAwaySelectedIndex = 3;
    private const int HighlightIndex = 4;
    private const string CancelId = "menu.cancel";
    private const float ManagementWidth = 170f;
    private const float BarGap = 6f;
    private const float HeaderHeight = 30f;
    private const float HeaderMinFontSize = 12f;
    private const float FarEnough = 200f;
    private const int MaxRows = 150;
    private static readonly TimeSpan ReloadInterval = TimeSpan.FromSeconds(1);
    private static readonly Color RestTint = new Color(1f, 1f, 1f, 0.35f);
    private static readonly Color MarkTint = new Color(1f, 0.8f, 0.3f, 1f);
    private static readonly Color SearchIdle = new Color(0.62f, 0.5f, 0.28f, 0.75f);
    private static readonly Color SearchIconTint = new Color(1f, 0.85f, 0.45f, 0.85f);
    private static readonly Color SearchFocused = new Color(1f, 0.8f, 0.3f, 0.9f);

    private static readonly float[] ButtonWidths = { 150f, 186f, 110f, 210f, 120f, 90f, 130f, 160f, 108f, 110f, CloseWidth };
    private static readonly Button?[] MenuButtons = new Button?[11];
    private static readonly TMP_Text?[] Headings = new TMP_Text?[2];
    private static readonly List<RowView> RowViews = new List<RowView>();
    private static readonly Dictionary<ContainerId, Container> Loaded = new Dictionary<ContainerId, Container>();
    private static readonly List<Container> Scratch = new List<Container>();
    private static readonly Vector3[] Corners = new Vector3[4];
    private static readonly Dictionary<string, string> ItemNames = new Dictionary<string, string>(StringComparer.Ordinal);
    private static readonly Dictionary<string, Sprite?> ItemIcons = new Dictionary<string, Sprite?>(StringComparer.Ordinal);
    private static readonly List<ContainerSnapshot> Fresh = new List<ContainerSnapshot>();

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
    private static Button? cancelButton;
    private static int selectionState = -1;
    private static Button? shareButton;
    private static Button? markButton;
    private static RectTransform? searchBoxRect;
    private static RectTransform? searchAreaRect;
    private static RectTransform? searchClearRect;
    private static RectTransform? listViewRect;
    private static TMP_InputField? searchInput;
    private static Outline? searchOutline;
    private static GameObject? searchClear;
    private static TMP_Text? searchEmpty;
    private static string query = string.Empty;
    private static bool marking;
    private static TimeSpan lastLoad = TimeSpan.Zero;
    private static IReadOnlyList<BaseMenuRow> rows = Array.Empty<BaseMenuRow>();

    /// <summary>Gets a value indicating whether a left click on the inventory marks favourites.</summary>
    internal static bool MarkMode => open && marking;

    /// <summary>Gets a value indicating whether the search box must keep the screen open. Esc takes the focus from the box first, and the game still closes it when the player cannot stay.</summary>
    internal static bool TypingBlocksClose =>
        searchInput != null
        && searchInput.isFocused
        && Player.m_localPlayer != null
        && !Player.m_localPlayer.IsDead()
        && !Player.m_localPlayer.IsTeleporting();

    /// <summary>Gets a value indicating whether the menu is open.</summary>
    internal static bool IsOpen => open && panel != null && panel.activeSelf;

    /// <summary>Gets the buttons in one row of the gamepad's focus.</summary>
    internal static int ButtonColumns => 5;

    /// <summary>Gets the search box, for the gamepad's highlight.</summary>
    internal static Selectable? SearchTarget => searchInput;

    /// <summary>Lists the menu's controls for the gamepad: its buttons, then each visible row's two cells.</summary>
    /// <param name="add">Receives each button and what pressing it does.</param>
    /// <param name="addRow">Receives each row cell and what pressing it does.</param>
    internal static void CopyTargets(Action<Selectable?, Action> add, Action<Selectable?, Action> addRow)
    {
        for (int i = 0; i < MenuButtons.Length; i++)
        {
            Button? button = MenuButtons[i];
            if (button != null && button.gameObject.activeInHierarchy)
            {
                add(button, button.onClick.Invoke);
            }
        }

        for (int i = 0; i < rows.Count && i < RowViews.Count; i++)
        {
            int index = i;
            addRow(RowViews[i].OpenTarget, () => OpenChest(index));
            addRow(RowViews[i].RuleTarget, () => CycleRule(index));
        }
    }

    /// <summary>Opens or closes the menu from the gamepad.</summary>
    internal static void ToggleFromGamepad() => OnOpenClick();

    /// <summary>Scrolls the list to a row.</summary>
    /// <param name="row">The row.</param>
    internal static void ScrollToRow(int row) => ScrollTo(row);

    /// <summary>Opens the game's text entry for the search box. A gamepad gets the game's own on-screen keyboard from it.</summary>
    internal static void AskSearchText()
    {
        TextInput? input = TextInput.instance;
        if (input != null && searchInput != null)
        {
            input.RequestText(new SearchReceiver(), StringTable.English.Get(StringKeys.MenuSearchTopic), 64);
        }
    }

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
        if (resumed)
        {
            SetMarking(false);
            DepositSelection.Cancel();
            RefreshSelection();
        }

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
            ShowInFullColour(clone, picture);
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

    /// <summary>The row's gold tint and raven shadow would muddy a coloured emblem, so hide the shadow and only brighten on hover.</summary>
    private static void ShowInFullColour(Button clone, Image picture)
    {
        picture.color = Color.white;
        Image? root = clone.GetComponent<Image>();
        if (root != null && root != picture)
        {
            root.color = Color.clear;
        }

        ColorBlock colors = clone.colors;
        colors.normalColor = IconRestColour;
        colors.selectedColor = IconRestColour;
        colors.highlightedColor = Color.white;
        colors.pressedColor = IconRestColour;
        clone.colors = colors;
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

    /// <summary>Opens the menu again after a window that replaced it closes.</summary>
    internal static void Reopen()
    {
        if (!open)
        {
            Toggle();
        }
    }

    /// <summary>Shows select mode on the buttons: what the next press does, and a Cancel that takes Highlight's place while it lasts.</summary>
    internal static void RefreshSelection()
    {
        Player? player = Player.m_localPlayer;
        Button? button = MenuButtons[PutAwaySelectedIndex];
        if (player == null || button == null)
        {
            return;
        }

        bool selecting = DepositSelection.SelectMode;
        int state = selecting ? DepositSelection.MovableCount(player) : -1;
        if (state != selectionState)
        {
            selectionState = state;
            if (state < 0)
            {
                VanillaButtonFactory.SetLabel(button, StringKeys.MenuPutAwaySelected);
            }
            else if (state == 0)
            {
                VanillaButtonFactory.SetLabel(button, StringKeys.MenuSelectItems);
            }
            else if (state == 1)
            {
                VanillaButtonFactory.SetLabel(button, StringKeys.MenuPutAwayCountOne);
            }
            else
            {
                VanillaButtonFactory.SetLabel(button, StringKeys.MenuPutAwayCountOther, state);
            }
        }

        if (cancelButton != null && cancelButton.gameObject.activeSelf != selecting)
        {
            cancelButton.gameObject.SetActive(selecting);
        }

        if (highlightButton != null && highlightButton.gameObject.activeSelf == selecting)
        {
            highlightButton.gameObject.SetActive(!selecting);
        }
    }

    private static void OnCancelClick() => PatchSafety.Run(ClickKey, static () =>
    {
        DepositSelection.Cancel();
        RefreshSelection();
    });

    private static void Toggle()
    {
        DepositSelection.Cancel();
        FeedPanel.Close();
        open = !open;
        SetMarking(false);
        if (!open && searchInput != null)
        {
            searchInput.text = string.Empty;
        }

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
        PanelStyle.Wood(panel.GetComponent<Image>(), gui);

        header = Text(gui, panel.transform, HeaderId, Margin, -Margin, MaxWidth - (3 * Margin) - CloseWidth, 30f);
        Transform parent = panel.transform;
        // Not cloned from Craft: a clone keeps the colours its template had at that moment as its normal look, and Craft is often greyed out.
        Button? organize = VanillaButtonFactory.Create(gui.m_stackAllButton, parent, OrganizeId, StringKeys.StationActionsOrganizeButton, OnOrganizeClick);
        Button? deposit = VanillaButtonFactory.Create(gui.m_stackAllButton, parent, DepositId, StringKeys.StationActionsDepositButton, OnDepositClick);
        Button? restock = VanillaButtonFactory.Create(gui.m_stackAllButton, parent, RestockId, StringKeys.MenuRestock, OnRestockClick);
        markButton = VanillaButtonFactory.Create(gui.m_stackAllButton, parent, MarkId, StringKeys.MenuMark, OnMarkClick);
        Button? close = VanillaButtonFactory.Create(gui.m_stackAllButton, parent, CloseId, StringKeys.MenuClose, OnOpenClick);
        takeButton = VanillaButtonFactory.Create(gui.m_takeAllButton, parent, TakeId, StringKeys.MenuTakeAll, OnTakeClick);
        placeButton = VanillaButtonFactory.Create(gui.m_stackAllButton, parent, PlaceId, StringKeys.MenuPlaceStacks, OnPlaceClick);
        highlightButton = VanillaButtonFactory.Create(gui.m_stackAllButton, parent, HighlightId, StringKeys.MenuHighlight, OnHighlightClick);
        shareButton = VanillaButtonFactory.Create(gui.m_stackAllButton, parent, ShareId, StringKeys.MenuShare, OnShareClick);

        // Grouped by what they act on: the base, then the open chest. Close sits in the panel's top corner. Moving and naming live in the All bases window.
        MenuButtons[0] = organize;
        MenuButtons[1] = deposit;
        MenuButtons[2] = restock;
        MenuButtons[3] = VanillaButtonFactory.Create(gui.m_stackAllButton, parent, PutAwaySelectedId, StringKeys.MenuPutAwaySelected, OnPutAwaySelectedClick);
        MenuButtons[4] = highlightButton;
        MenuButtons[5] = markButton;
        MenuButtons[6] = takeButton;
        MenuButtons[7] = placeButton;
        MenuButtons[8] = shareButton;
        MenuButtons[9] = VanillaButtonFactory.Create(gui.m_stackAllButton, parent, AllBasesId, StringKeys.MenuAllBases, OnAllBasesClick);
        MenuButtons[CloseIndex] = close;
        selectionState = -1;
        cancelButton = VanillaButtonFactory.Create(gui.m_stackAllButton, parent, CancelId, StringKeys.MenuCancel, OnCancelClick);
        if (cancelButton != null)
        {
            cancelButton.gameObject.SetActive(false);
        }
        Headings[0] = Heading(gui, HeadingBaseId, StringKeys.MenuHeadingBase);
        Headings[1] = Heading(gui, HeadingChestId, StringKeys.MenuHeadingChest);
        for (int i = 0; i < MenuButtons.Length; i++)
        {
            Size(MenuButtons[i], ButtonWidths[i]);
        }

        CreateSearch(gui);
        CreateList(gui);
        Fit(gui);
    }

    private static TMP_Text? Heading(InventoryGui gui, string id, string key)
    {
        TMP_Text? text = Text(gui, panel!.transform, id, 0f, 0f, MaxWidth, MenuButtonLayout.HeadingHeight);
        if (text != null)
        {
            text.fontSize = 15f;
            text.alpha = 0.75f;
            text.text = StringTable.English.Get(key);
        }

        return text;
    }

    /// <summary>Gives a button its design width and height once; where it sits is the Core layout's decision on every Fit.</summary>
    private static void Size(Button? button, float width)
    {
        if (button != null)
        {
            VanillaButtonFactory.Pin((RectTransform)button.transform, new Vector2(width, ButtonHeight), Vector2.zero);
            button.interactable = true;
            button.gameObject.SetActive(true);
        }
    }

    private static void Fit(InventoryGui gui)
    {
        var rect = (RectTransform)panel!.transform;
        FitBetween(gui, rect, PanelHeight);
        ApplyLayout(rect.sizeDelta.x);
    }

    /// <summary>
    /// Places the buttons, the search box and the list from <see cref="MenuButtonLayout"/>, using each button's measured width. Every
    /// control is pinned at (X, -Top) from the panel's top-left corner, so the buttons wrap at a narrow panel and nothing overlaps.
    /// </summary>
    private static void ApplyLayout(float panelWidth)
    {
        var specs = new MenuButtonSpec[ManagementIndex];
        for (int i = 0; i < specs.Length; i++)
        {
            Button? button = MenuButtons[i];
            float measured = button == null ? 0f : ((RectTransform)button.transform).sizeDelta.x;
            specs[i] = new MenuButtonSpec(measured > 0f ? measured : ButtonWidths[i], i == 3, i == 0 || i == TakeIndex);
        }

        float searchHeight = searchBoxRect == null || searchBoxRect.sizeDelta.y <= 0f ? SearchHeight : searchBoxRect.sizeDelta.y;
        MenuLayout layout = MenuButtonLayout.Arrange(panelWidth, PanelHeight, specs, searchHeight);
        for (int i = 0; i < specs.Length; i++)
        {
            Button? button = MenuButtons[i];
            if (button != null)
            {
                MenuRect rect = layout.Buttons[i];
                VanillaButtonFactory.Pin((RectTransform)button.transform, new Vector2(rect.Width, rect.Height), new Vector2(rect.X, -rect.Top));
            }
        }

        for (int i = 0; i < Headings.Length && i < layout.Headings.Count; i++)
        {
            TMP_Text? heading = Headings[i];
            if (heading != null)
            {
                MenuRect rect = layout.Headings[i];
                VanillaButtonFactory.Pin((RectTransform)heading.transform, new Vector2(rect.Width, rect.Height), new Vector2(rect.X, -rect.Top));
            }
        }

        Button? closeButton = MenuButtons[CloseIndex];
        if (closeButton != null)
        {
            VanillaButtonFactory.Pin((RectTransform)closeButton.transform, new Vector2(CloseWidth, CloseHeight), new Vector2(panelWidth - Margin - CloseWidth, -Margin));
        }

        if (cancelButton != null)
        {
            MenuRect slot = layout.Buttons[HighlightIndex];
            VanillaButtonFactory.Pin((RectTransform)cancelButton.transform, new Vector2(slot.Width, slot.Height), new Vector2(slot.X, -slot.Top));
        }

        float managementX = panelWidth - Margin - CloseWidth - BarGap - ManagementWidth;
        Button? management = MenuButtons[ManagementIndex];
        if (management != null)
        {
            VanillaButtonFactory.Pin((RectTransform)management.transform, new Vector2(ManagementWidth, CloseHeight), new Vector2(managementX, -Margin));
        }

        if (header != null)
        {
            // The status line shrinks on a narrow panel instead of running under the top bar's buttons.
            VanillaButtonFactory.Pin((RectTransform)header.transform, new Vector2(Math.Max(0f, managementX - Margin - BarGap), HeaderHeight), new Vector2(Margin, -Margin));
            header.enableAutoSizing = true;
            header.fontSizeMin = HeaderMinFontSize;
            header.fontSizeMax = Math.Max(header.fontSizeMax, HeaderMinFontSize);
        }

        MenuRect search = layout.Search;
        if (searchBoxRect != null)
        {
            VanillaButtonFactory.Pin(searchBoxRect, new Vector2(search.Width, search.Height), new Vector2(search.X, -search.Top));
            if (searchAreaRect != null)
            {
                float areaLeft = SearchPadding + SearchIconSize + SearchPadding;
                VanillaButtonFactory.Pin(searchAreaRect, new Vector2(Math.Max(0f, search.Width - areaLeft - SearchClearWidth - SearchPadding), search.Height), new Vector2(areaLeft, 0f));
            }

            if (searchClearRect != null)
            {
                VanillaButtonFactory.Pin(searchClearRect, new Vector2(SearchClearWidth - 6f, search.Height - 8f), new Vector2(search.Width - SearchClearWidth + 2f, -4f));
            }
        }

        MenuRect list = layout.List;
        if (listViewRect != null)
        {
            VanillaButtonFactory.Pin(listViewRect, new Vector2(list.Width, list.Height), new Vector2(list.X, -list.Top));
        }
    }

    /// <summary>Sits a panel between the chest window and the crafting panel, measured from both.</summary>
    /// <param name="gui">The inventory GUI.</param>
    /// <param name="rect">The panel.</param>
    /// <param name="height">The panel's height.</param>
    internal static void FitBetween(InventoryGui gui, RectTransform rect, float height)
    {
        var parent = (RectTransform)rect.parent;
        float left = RightOfPlayerInfo(gui, parent) + PanelGap;
        float right = LocalEdge(gui.m_crafting, parent, right: false) - PanelGap;
        float width = Mathf.Clamp(right - left, MinWidth, MaxWidth);
        rect.sizeDelta = new Vector2(width, height);
        Rect bounds = parent.rect;
        rect.anchoredPosition = new Vector2(left - bounds.xMin, -PanelGap);
    }

    /// <summary>The right edge of the chest window, the inventory and its weight and armor readouts, so the panel covers none of them.</summary>
    private static float RightOfPlayerInfo(InventoryGui gui, RectTransform parent)
    {
        float edge = LocalEdge(gui.m_container, parent, right: true);
        edge = Mathf.Max(edge, LocalEdge(gui.m_player, parent, right: true));
        if (gui.m_weight != null)
        {
            edge = Mathf.Max(edge, LocalEdge(gui.m_weight.rectTransform, parent, right: true));
        }

        if (gui.m_armor != null)
        {
            edge = Mathf.Max(edge, LocalEdge(gui.m_armor.rectTransform, parent, right: true));
        }

        return edge;
    }

    private static float LocalEdge(RectTransform target, RectTransform parent, bool right)
    {
        target.GetWorldCorners(Corners);
        return parent.InverseTransformPoint(Corners[right ? 2 : 0]).x;
    }

    /// <summary>
    /// Builds the search box by hand, as a standard input field: the text sits in its own padded area, because the field scrolls its first
    /// letter flush to that area's edge. The text is new rather than cloned, because a clone brings its template's margins with it.
    /// </summary>
    private static void CreateSearch(InventoryGui gui)
    {
        float width = MaxWidth - (2 * Margin);
        GameObject box = VanillaButtonFactory.CreateNode(panel!.transform, SearchId, new Vector2(width, SearchHeight), Vector2.zero, typeof(RectTransform), typeof(Image));
        searchBoxRect = (RectTransform)box.transform;
        box.SetActive(false);
        box.GetComponent<Image>().color = PanelStyle.Field;
        searchOutline = box.AddComponent<Outline>();
        searchOutline.effectColor = SearchIdle;
        searchOutline.effectDistance = new Vector2(2f, -2f);
        Image glass = VanillaButtonFactory.CreateNode(box.transform, SearchIconId, new Vector2(SearchIconSize, SearchIconSize), new Vector2(SearchPadding, -(SearchHeight - SearchIconSize) / 2f), typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        glass.sprite = SearchIcon.Sprite;
        glass.color = SearchIconTint;
        glass.raycastTarget = false;
        float areaLeft = SearchPadding + SearchIconSize + SearchPadding;
        var area = (RectTransform)VanillaButtonFactory.CreateNode(box.transform, SearchAreaId, new Vector2(width - areaLeft - SearchClearWidth - SearchPadding, SearchHeight), new Vector2(areaLeft, 0f), typeof(RectTransform), typeof(RectMask2D)).transform;
        searchAreaRect = area;
        TMP_Text? text = SearchText(gui, area, SearchTextId);
        TMP_Text? hint = SearchText(gui, area, SearchHintId);
        if (text == null || hint == null)
        {
            return;
        }

        hint.text = StringTable.English.Get(StringKeys.MenuSearch);
        hint.fontStyle = FontStyles.Italic;
        hint.alpha = 0.6f;
        TMP_InputField input = box.AddComponent<TMP_InputField>();
        input.transition = Selectable.Transition.None;
        input.textViewport = area;
        input.textComponent = text;
        input.placeholder = hint;
        input.lineType = TMP_InputField.LineType.SingleLine;
        input.customCaretColor = true;
        input.caretColor = SearchFocused;
        input.caretWidth = 2;
        input.selectionColor = new Color(1f, 0.8f, 0.3f, 0.35f);
        input.onValueChanged.AddListener(OnSearchChanged);
        input.onSelect.AddListener(static _ => SetSearchFocus(true));
        input.onDeselect.AddListener(static _ => SetSearchFocus(false));
        searchInput = input;
        CreateSearchClear(gui, box.transform, width);
        box.SetActive(true);
    }

    private static void SetSearchFocus(bool focused)
    {
        if (searchOutline != null)
        {
            searchOutline.effectColor = focused ? SearchFocused : SearchIdle;
        }
    }

    private static void CreateSearchClear(InventoryGui gui, Transform box, float width)
    {
        GameObject clear = VanillaButtonFactory.CreateNode(box, SearchClearId, new Vector2(SearchClearWidth - 6f, SearchHeight - 8f), new Vector2(width - SearchClearWidth + 2f, -4f), typeof(RectTransform), typeof(Image), typeof(Button));
        clear.GetComponent<Image>().color = new Color(1f, 0.85f, 0.45f, 0.18f);
        clear.GetComponent<Button>().onClick.AddListener(static () => PatchSafety.Run(ClickKey, static () =>
        {
            if (searchInput != null)
            {
                searchInput.text = string.Empty;
                searchInput.ActivateInputField();
            }
        }));
        TMP_Text? label = Text(gui, clear.transform, SearchClearLabelId, 0f, 0f, SearchClearWidth - 6f, SearchHeight - 8f, gui.m_recipeDecription);
        if (label != null)
        {
            ContentSizeFitter? fitter = label.GetComponent<ContentSizeFitter>();
            if (fitter != null)
            {
                fitter.enabled = false;
            }

            label.alignment = TextAlignmentOptions.Center;
            label.fontSize = 15f;
            label.text = StringTable.English.Get(StringKeys.MenuSearchClear);
        }

        clear.SetActive(false);
        searchClear = clear;
        searchClearRect = (RectTransform)clear.transform;
    }

    private static TMP_Text? SearchText(InventoryGui gui, Transform area, string id)
    {
        TMP_Text? template = gui.m_recipeDecription;
        if (template == null)
        {
            return null;
        }

        GameObject node = VanillaButtonFactory.CreateNode(area, id, Vector2.zero, Vector2.zero, typeof(RectTransform), typeof(TextMeshProUGUI));
        var rect = (RectTransform)node.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        TMP_Text text = node.GetComponent<TMP_Text>();
        text.font = template.font;
        text.fontSize = 20f;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Overflow;
        text.richText = false;
        text.raycastTarget = false;
        return text;
    }

    private static void OnSearchChanged(string value) => PatchSafety.Run(ClickKey, value, static text =>
    {
        query = text.Trim();
        lastLoad = TimeSpan.Zero;
        if (searchClear != null)
        {
            searchClear.SetActive(text.Length > 0);
        }

        if (listContent != null)
        {
            listContent.anchoredPosition = new Vector2(listContent.anchoredPosition.x, 0f);
        }
    });

    private static void CreateList(InventoryGui gui)
    {
        float width = MaxWidth - (2 * Margin);
        GameObject view = VanillaButtonFactory.CreateNode(panel!.transform, ListId, new Vector2(width, RowHeight), Vector2.zero, typeof(RectTransform), typeof(RectMask2D), typeof(ScrollRect), typeof(Image));
        var viewRect = (RectTransform)view.transform;
        listViewRect = viewRect;
        view.GetComponent<Image>().color = PanelStyle.Inset;
        listContent = (RectTransform)VanillaButtonFactory.CreateNode(view.transform, RowsId, new Vector2(width, 0f), Vector2.zero, typeof(RectTransform)).transform;
        ScrollRect scroll = view.GetComponent<ScrollRect>();
        listScroll = scroll;
        scroll.content = listContent;
        scroll.viewport = viewRect;
        scroll.horizontal = false;
        scroll.scrollSensitivity = PanelStyle.ScrollSpeed;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        for (int i = 0; i < MaxRows; i++)
        {
            RowViews.Add(new RowView(gui, listContent, i, width));
        }

        searchEmpty = Text(gui, view.transform, SearchEmptyId, Margin, -Margin, width - (2 * Margin), RowHeight);
        if (searchEmpty != null)
        {
            searchEmpty.alignment = TextAlignmentOptions.Center;
            searchEmpty.alpha = 0.7f;
            searchEmpty.gameObject.SetActive(false);
        }
    }

    private static TMP_Text? Text(InventoryGui gui, Transform parent, string id, float x, float y, float width, float height, TMP_Text? template = null)
    {
        TMP_Text? text = VanillaButtonFactory.CreateText(template == null ? gui.m_craftingStationName : template, parent, id);
        if (text != null)
        {
            VanillaButtonFactory.Pin((RectTransform)text.transform, new Vector2(width, height), new Vector2(x, y));
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.enableAutoSizing = false;
            text.fontSize = 18f;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.raycastTarget = false;
            text.richText = true;
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
        var names = new Dictionary<ContainerId, string>();
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

                string name = ChestNames.Read(chest);
                if (name.Length > 0)
                {
                    names[id] = name;
                }
            }
        }

        rows = BaseMenuBuilder.Rows(query.Length == 0 ? resolution.Containers : CurrentContents(current, resolution.Containers), assignments, names, position, null, StringTable.English);
        if (query.Length == 0)
        {
            SetText(header, StringTable.English.Format(StringKeys.MenuHeader, resolution.Stations.Count, resolution.Containers.Count, BaseMenuBuilder.FillPercent(resolution.Containers)));
        }
        else
        {
            rows = BaseSearch.Apply(rows, query, NameOf, CategoryOf);
            SetText(header, SearchHeader());
        }

        Show();
    }

    private static string SearchHeader()
    {
        StringTable strings = StringTable.English;
        if (rows.Count == 0)
        {
            return strings.Get(StringKeys.MenuSearchNoneHeader);
        }

        IReadOnlyList<string> items = BaseSearch.Items(rows);
        string chests = strings.FormatPlural(StringKeys.MenuSearchChests, rows.Count, rows.Count);
        if (items.Count == 0)
        {
            return chests;
        }

        return items.Count == 1
            ? strings.Format(StringKeys.MenuSearchHeaderOne, items[0], BaseSearch.Total(rows), chests)
            : strings.Format(StringKeys.MenuSearchHeader, items.Count, BaseSearch.Total(rows), chests);
    }

    /// <summary>Reads each chest as it is now, because a search must not show the base cache's older contents.</summary>
    private static List<ContainerSnapshot> CurrentContents(PluginWorld current, IReadOnlyList<ContainerSnapshot> containers)
    {
        Fresh.Clear();
        for (int i = 0; i < containers.Count; i++)
        {
            ContainerSnapshot? fresh = Loaded.TryGetValue(containers[i].Id, out Container? chest) ? current.Snapshots.Snapshot(chest) : null;
            Fresh.Add(fresh ?? containers[i]);
        }

        return Fresh;
    }

    private static string NameOf(string prefab)
    {
        if (!ItemNames.TryGetValue(prefab, out string? name))
        {
            ItemDrop? drop = DropOf(prefab);
            name = drop == null || Localization.instance == null ? prefab : Localization.instance.Localize(drop.m_itemData.m_shared.m_name);
            ItemNames[prefab] = name;
        }

        return name;
    }

    private static string CategoryOf(string prefab)
    {
        ItemDrop? drop = DropOf(prefab);
        ItemKind kind = drop == null ? ItemKind.Other : ItemKindAdapter.From(drop.m_itemData.m_shared);
        return PluginCategoryCatalog.Current.Resolve(prefab, kind);
    }

    private static Sprite? IconOf(string prefab)
    {
        if (!ItemIcons.TryGetValue(prefab, out Sprite? icon))
        {
            ItemDrop? drop = DropOf(prefab);
            icon = drop == null ? null : drop.m_itemData.GetIcon();
            ItemIcons[prefab] = icon;
        }

        return icon;
    }

    private static ItemDrop? DropOf(string prefab)
    {
        GameObject? item = ObjectDB.instance == null ? null : ObjectDB.instance.GetItemPrefab(prefab);
        return item == null ? null : item.GetComponent<ItemDrop>();
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
        SetEnabled(highlightButton, current != null || (query.Length > 0 && rows.Count > 0));
        SetEnabled(shareButton, current != null);
        RefreshSelection();
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

        ShowSearchEmpty();
        if (listContent != null)
        {
            listContent.sizeDelta = new Vector2(listContent.sizeDelta.x, Math.Min(rows.Count, MaxRows) * RowHeight);
            if (scrollPending && openRow >= 0)
            {
                ScrollTo(openRow);
            }
        }
    }

    private static void ShowSearchEmpty()
    {
        if (searchEmpty == null)
        {
            return;
        }

        bool none = query.Length > 0 && rows.Count == 0;
        if (searchEmpty.gameObject.activeSelf != none)
        {
            searchEmpty.gameObject.SetActive(none);
        }

        if (none)
        {
            SetText(searchEmpty, StringTable.English.Format(StringKeys.MenuSearchNone, query));
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

    private static void CycleRule(int index)
    {
        if (index < rows.Count && Loaded.TryGetValue(rows[index].Chest.Id, out Container? chest))
        {
            ChestCategoryButton.Cycle(chest, true);
        }
    }

    private static void OnTakeClick() => PatchSafety.Run(ClickKey, static () => builtFor?.OnTakeAll());

    private static void OnPlaceClick() => PatchSafety.Run(ClickKey, static () => builtFor?.OnStackAll());

    private static void OnMarkClick() => PatchSafety.Run(ClickKey, static () => SetMarking(!marking));

    private static void SetMarking(bool on)
    {
        marking = on;
        if (markButton != null)
        {
            ColorBlock colors = markButton.colors;
            colors.normalColor = on ? MarkTint : Color.white;
            colors.selectedColor = colors.normalColor;
            markButton.colors = colors;
        }
    }

    private static void OnHighlightClick() => PatchSafety.Run(ClickKey, static () =>
    {
        if (query.Length > 0)
        {
            var found = new List<Container>(rows.Count);
            for (int i = 0; i < rows.Count; i++)
            {
                if (Loaded.TryGetValue(rows[i].Chest.Id, out Container? match))
                {
                    found.Add(match);
                }
            }

            ChestHighlight.ShowAll(found);
            return;
        }

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

    private static void OnPutAwaySelectedClick() => PatchSafety.Run(ClickKey, static () =>
    {
        Player player = Player.m_localPlayer;
        if (player != null)
        {
            Vector3 at = player.transform.position;
            DepositSelection.OnButton(player, BaseStart.FromPosition(new WorldPosition(at.x, at.y, at.z)));
            RefreshSelection();
            lastLoad = TimeSpan.Zero;
        }
    });

    private static void OnAllBasesClick() => PatchSafety.Run(ClickKey, BaseStorage.Plugin.BaseMove.BasesWindow.Open);

    private static void OnRestockClick() => PatchSafety.Run(ClickKey, static () => StartRestock());

    private static void StartRestock()
    {
        Player player = Player.m_localPlayer;
        if (player == null)
        {
            return;
        }

        // The coordinator has already shown any notice; the outcome needs nothing more here.
        Vector3 at = player.transform.position;
        OrganizeStartOutcome _ = OrganizeRunner.StartRestockAt(new WorldPosition(at.x, at.y, at.z));
        lastLoad = TimeSpan.Zero;
    }

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

    private sealed class SearchReceiver : TextReceiver
    {
        public string GetText() => query;

        public void SetText(string text)
        {
            if (searchInput != null)
            {
                searchInput.text = text ?? string.Empty;
            }
        }
    }

    private sealed class RowView
    {
        private readonly GameObject root;
        private readonly Image background;
        private readonly Button click;
        private readonly Button? ruleClick;
        private readonly TMP_Text? label;
        private readonly TMP_Text? rule;
        private readonly TMP_Text? openHint;
        private readonly Image icon;
        private readonly float labelWidth;
        private readonly List<Image> previews = new List<Image>();
        private readonly InventoryGui gui;
        private TMP_Text? more;
        private Image? fillBar;
        private ContainerSnapshot? previewChest;
        private ChestPreview? preview;
        private float labelX = 6f;
        private float labelSpan;
        private bool lit;

        internal RowView(InventoryGui gui, Transform parent, int index, float width)
        {
            this.gui = gui;
            labelWidth = width - RuleWidth - 2f;
            labelSpan = labelWidth - OpenHintWidth - 10f;
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
            icon = VanillaButtonFactory.CreateNode(root.transform, RowIconId, new Vector2(RowIconSize, RowIconSize), new Vector2(4f, -3f), typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            icon.raycastTarget = false;
            icon.preserveAspect = true;
            icon.gameObject.SetActive(false);
            openHint = Text(gui, root.transform, OpenHintId, labelWidth - OpenHintWidth, 0f, OpenHintWidth - 4f, RowHeight - 2f);
            if (openHint != null)
            {
                openHint.alignment = TextAlignmentOptions.MidlineRight;
                openHint.alpha = 0.6f;
            }

            RectTransform? cycleRect = null;
            GameObject cycle = Cell(root.transform, RuleId, RuleWidth, width - RuleWidth, () => OpenRules(index, cycleRect!));
            cycleRect = (RectTransform)cycle.transform;
            ruleClick = cycle.GetComponent<Button>();
            rule = Text(gui, cycle.transform, RuleLabelId, 6f, 0f, RuleWidth - 10f, RowHeight - 2f);
            root.SetActive(false);
        }

        private static string HitText(BaseMenuRow row)
        {
            if (row.Hits.Count == 0)
            {
                return string.Empty;
            }

            string hit = StringTable.English.Format(StringKeys.MenuHit, BaseSearch.Mark(row.Hits[0].Name, query, MatchOpen, MatchClose), row.Hits[0].Count);
            return row.Hits.Count == 1 ? hit : StringTable.English.Format(StringKeys.MenuHitMore, hit, row.Hits.Count - 1);
        }

        private static string WithName(BaseMenuRow row, string text) =>
            row.Name.Length == 0 ? text : BaseSearch.Mark(row.Name, query, MatchOpen, MatchClose) + "  " + text;

        private static GameObject Cell(Transform parent, string id, float width, float x, Action click)
        {
            GameObject cell = VanillaButtonFactory.CreateNode(parent, id, new Vector2(width, RowHeight - 2f), new Vector2(x, 0f), typeof(RectTransform), typeof(Image), typeof(Button));
            cell.GetComponent<Image>().color = PanelStyle.Inset;
            cell.GetComponent<Button>().onClick.AddListener(() => PatchSafety.Run(ClickKey, click));
            return cell;
        }

        internal Selectable OpenTarget => click;

        internal Selectable? RuleTarget => ruleClick;

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

            background.color = isOpen ? new Color(1f, 0.8f, 0.3f, 0.6f) : PanelStyle.Inset;
            int distance = (int)Mathf.Sqrt(row.DistanceSquared);
            bool found = row.Hits.Count > 0;
            ShowIcon(found ? IconOf(row.Hits[0].Prefab) : null);
            SetText(openHint, isOpen
                ? StringTable.English.Get(StringKeys.MenuClose)
                : found ? StringTable.English.Format(StringKeys.MenuDistance, distance) : StringTable.English.Get(StringKeys.MenuOpen));

            string state = row.AccessDenied ? StringTable.English.Get(StringKeys.MenuNoAccess) : row.InUse ? StringTable.English.Get(StringKeys.MenuInUse) : string.Empty;
            ShowFill(row.FillPercent);
            if (!found && state.Length == 0)
            {
                ShowPreview(row);
            }
            else
            {
                HidePreview(0);
                SetText(label, found
                    ? (state.Length == 0 ? HitText(row) : HitText(row) + "  (" + state + ")")
                    : WithName(row, StringTable.English.Format(StringKeys.MenuRow, row.FillPercent, (int)Mathf.Sqrt(row.DistanceSquared), state)));
            }

            SetText(rule, BaseSearch.Mark(row.RuleLabel, query, MatchOpen, MatchClose));
        }

        /// <summary>Shows a found item's icon and moves the text clear of it. A result with no icon keeps the text in line with the others.</summary>
        private void ShowIcon(Sprite? sprite)
        {
            bool show = sprite != null;
            icon.sprite = sprite;
            if (icon.gameObject.activeSelf != show)
            {
                icon.gameObject.SetActive(show);
            }

            float x = query.Length > 0 ? RowIconSpace : 6f;
            PlaceLabel(x, labelWidth - OpenHintWidth - 4f - x);
        }

        private void PlaceLabel(float x, float span)
        {
            if (label != null && (x != labelX || span != labelSpan))
            {
                labelX = x;
                labelSpan = span;
                VanillaButtonFactory.Pin(label.rectTransform, new Vector2(span, RowHeight - 2f), new Vector2(x, 0f));
            }
        }

        /// <summary>Shows what's in the chest: its name if it has one, then its most plentiful items, then how many other kinds there are.</summary>
        private void ShowPreview(BaseMenuRow row)
        {
            bool named = row.Name.Length > 0;
            bool empty = row.Chest.Contents.Count == 0;
            float start = named ? NamedPreviewStart : 6f;
            if (named || empty)
            {
                PlaceLabel(6f, named ? NamedPreviewStart - 10f : labelWidth - OpenHintWidth - 10f);
                SetText(label, named ? BaseSearch.Mark(row.Name, query, MatchOpen, MatchClose) : StringTable.English.Get(StringKeys.MenuRowEmpty));
            }
            else
            {
                SetText(label, string.Empty);
            }

            if (!ReferenceEquals(previewChest, row.Chest) || preview == null)
            {
                previewChest = row.Chest;
                int fits = Mathf.Max(0, Mathf.FloorToInt((labelWidth - OpenHintWidth - 6f - start - PreviewMoreWidth) / PreviewSpace));
                preview = ChestPreview.Of(row.Chest.Contents, fits);
            }

            for (int i = 0; i < preview.Prefabs.Count; i++)
            {
                if (i == previews.Count)
                {
                    previews.Add(CreatePreview());
                }

                Image image = previews[i];
                image.sprite = IconOf(preview.Prefabs[i]);
                image.enabled = image.sprite != null;
                VanillaButtonFactory.Pin((RectTransform)image.transform, new Vector2(RowIconSize, RowIconSize), new Vector2(start + (i * PreviewSpace), -3f));
                if (!image.gameObject.activeSelf)
                {
                    image.gameObject.SetActive(true);
                }
            }

            HidePreview(preview.Prefabs.Count);
            ShowMore(preview.Hidden, start + (preview.Prefabs.Count * PreviewSpace));
        }

        private Image CreatePreview()
        {
            Image image = VanillaButtonFactory.CreateNode(root.transform, RowPreviewId, new Vector2(RowIconSize, RowIconSize), Vector2.zero, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            image.raycastTarget = false;
            image.preserveAspect = true;
            return image;
        }

        private void ShowMore(int hidden, float x)
        {
            if (hidden > 0 && more == null)
            {
                more = Text(gui, root.transform, RowMoreId, x, 0f, PreviewMoreWidth, RowHeight - 2f);
            }

            if (more != null)
            {
                more.gameObject.SetActive(hidden > 0);
                if (hidden > 0)
                {
                    VanillaButtonFactory.Pin(more.rectTransform, new Vector2(PreviewMoreWidth, RowHeight - 2f), new Vector2(x, 0f));
                    SetText(more, StringTable.English.Format(StringKeys.MenuRowMore, hidden));
                }
            }
        }

        private void HidePreview(int from)
        {
            for (int i = from; i < previews.Count; i++)
            {
                if (previews[i].gameObject.activeSelf)
                {
                    previews[i].gameObject.SetActive(false);
                }
            }

            if (from == 0 && more != null)
            {
                more.gameObject.SetActive(false);
            }
        }

        /// <summary>A thin bar along the row's bottom edge keeps how full the chest is visible at a glance.</summary>
        private void ShowFill(int percent)
        {
            if (fillBar == null)
            {
                fillBar = VanillaButtonFactory.CreateNode(root.transform, RowFillId, Vector2.zero, Vector2.zero, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                fillBar.raycastTarget = false;
                fillBar.color = new Color(1f, 0.8f, 0.3f, 0.75f);
            }

            VanillaButtonFactory.Pin(fillBar.rectTransform, new Vector2(labelWidth * percent / 100f, FillBarHeight), new Vector2(0f, -(RowHeight - 2f - FillBarHeight)));
        }
    }
}
