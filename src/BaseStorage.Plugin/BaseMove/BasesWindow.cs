namespace BaseStorage.Plugin.BaseMove;

using System;
using System.Collections.Generic;
using System.Globalization;
using BaseStorage.Core.BaseMove;
using BaseStorage.Core.Compendium;
using BaseStorage.Core.Snapshots;
using BaseStorage.Core.Ui;
using BaseStorage.Plugin.Patching;
using BaseStorage.Plugin.Ui;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The All bases window: every base in the world, wherever the player stands, with rename and move between any two. A child of the inventory
/// screen, like the compendium. The list comes from the host, the only machine holding far bases' records. Main thread only.
/// </summary>
internal static class BasesWindow
{
    /// <summary>The patch key for the window's tick.</summary>
    internal const string TickKey = "BaseStorage.Plugin.BaseMove.BasesWindow.Tick";

    private const string ClickKey = "BaseStorage.Plugin.BaseMove.BasesWindow.Click";
    private const string WindowId = "bases.window";
    private const string CloseId = "bases.close";
    private const string RefreshId = "bases.refresh";
    private const string StatusId = "bases.status";
    private const string ListId = "bases.list";
    private const string ListContentId = "bases.list.content";
    private const string RowId = "bases.row";
    private const string NameId = "bases.row.name";
    private const string InfoId = "bases.row.info";
    private const string RenameId = "bases.row.rename";
    private const string FromId = "bases.row.from";
    private const string ToId = "bases.row.to";
    private const float Width = 780f;
    private const float Height = 620f;
    private const float ScreenMargin = 30f;
    private const float Margin = 12f;
    private const float BarHeight = 38f;
    private const float BarButtonWidth = 110f;
    private const float RowHeight = 62f;
    private const float RowButtonWidth = 88f;
    private const float RowButtonHeight = 36f;
    private const int MaxRows = 40;

    private static readonly List<RowView> Rows = new List<RowView>();
    private static readonly Vector3[] Corners = new Vector3[4];
    private static readonly Color Lit = new Color(1f, 0.8f, 0.3f, 0.35f);
    private static readonly Color Rest = new Color(1f, 1f, 1f, 0.08f);

    private static RectTransform? root;
    private static RectTransform? content;
    private static TMP_Text? status;
    private static List<BaseEntry> entries = new List<BaseEntry>();
    private static ContainerId source;
    private static bool loading;

    private static bool reopenBaseMenu;

    /// <summary>Gets a value indicating whether the window is open.</summary>
    internal static bool IsOpen => root != null && root.gameObject.activeSelf;

    /// <summary>Opens the window and asks for the list.</summary>
    internal static void Open()
    {
        InventoryGui? gui = InventoryGui.instance;
        if (gui == null)
        {
            return;
        }

        if (root == null)
        {
            Build(gui);
        }

        // The window replaces the base menu rather than sitting on top of it, and gives it back on Close.
        reopenBaseMenu = BaseMenu.BaseMenuPanel.IsOpen;
        if (reopenBaseMenu)
        {
            BaseMenu.BaseMenuPanel.Hide();
        }

        root!.gameObject.SetActive(true);
        root.SetAsLastSibling();
        Place(gui);
        Refresh();
    }

    /// <summary>Closes the window when the inventory has closed.</summary>
    internal static void Tick() => PatchSafety.Run(TickKey, static () =>
    {
        if (IsOpen && !InventoryGui.IsVisible())
        {
            reopenBaseMenu = false;
            root!.gameObject.SetActive(false);
        }
    });

    /// <summary>Takes a new list from the host.</summary>
    /// <param name="bases">The bases.</param>
    internal static void ShowList(IReadOnlyList<BaseEntry> bases)
    {
        loading = false;
        Player? player = Player.m_localPlayer;
        Vector3 at = player == null ? Vector3.zero : player.transform.position;
        entries = BaseDirectory.ForPlayer(bases, at.x, at.z, FallbackName);
        ContainerId here = PluginBaseMove.StationHereId();
        if (!Has(source))
        {
            source = default;
            foreach (BaseEntry entry in entries)
            {
                if (Contains(entry, here))
                {
                    source = entry.Station;
                }
            }
        }

        Show();
    }

    private static string FallbackName(CompendiumBiome? biome, int number) =>
        StringTable.English.Format(StringKeys.BasesFallbackName, biome.HasValue ? CompendiumBuilder.BiomeName(biome.Value) : StringTable.English.Get(StringKeys.BasesUnknownBiome), number);

    private static bool Contains(BaseEntry entry, ContainerId station)
    {
        if (station.IsNone)
        {
            return false;
        }

        for (int i = 0; i < entry.Stations.Count; i++)
        {
            if (entry.Stations[i] == station)
            {
                return true;
            }
        }

        return false;
    }

    private static bool Has(ContainerId station) => !station.IsNone && entries.Exists(e => e.Station == station);

    private static void Refresh()
    {
        loading = true;
        Show();
        PluginBaseMove.AskBases(ShowList);
    }

    private static void Build(InventoryGui gui)
    {
        Rows.Clear();
        source = default;
        GameObject node = VanillaButtonFactory.CreateNode(gui.m_inventoryRoot, WindowId, new Vector2(Width, Height), Vector2.zero, typeof(RectTransform), typeof(Image));
        PanelStyle.Wood(node.GetComponent<Image>(), gui);
        root = (RectTransform)node.transform;
        status = Label(gui, root, StatusId, new Vector2(Width - (2f * BarButtonWidth) - (4f * Margin), BarHeight), new Vector2(Margin, -Margin), 20f);
        BarPin(VanillaButtonFactory.Create(gui.m_stackAllButton, root, RefreshId, StringKeys.BasesRefresh, static () => PatchSafety.Run(ClickKey, Refresh)), Width - (2f * BarButtonWidth) - (2f * Margin));
        BarPin(VanillaButtonFactory.Create(gui.m_stackAllButton, root, CloseId, StringKeys.CompendiumClose, static () => PatchSafety.Run(ClickKey, Close)), Width - BarButtonWidth - Margin);

        float listTop = -(2f * Margin) - BarHeight;
        var listSize = new Vector2(Width - (2f * Margin), Height + listTop - Margin);
        GameObject list = VanillaButtonFactory.CreateNode(root, ListId, listSize, new Vector2(Margin, listTop), typeof(RectTransform), typeof(Image), typeof(RectMask2D), typeof(ScrollRect));
        list.GetComponent<Image>().color = PanelStyle.Inset;
        GameObject contentNode = VanillaButtonFactory.CreateNode(list.transform, ListContentId, listSize, Vector2.zero, typeof(RectTransform));
        content = (RectTransform)contentNode.transform;
        ScrollRect scroll = list.GetComponent<ScrollRect>();
        scroll.viewport = (RectTransform)list.transform;
        scroll.content = content;
        scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.inertia = false;
        scroll.scrollSensitivity = PanelStyle.ScrollSpeed;
        for (int i = 0; i < MaxRows; i++)
        {
            Rows.Add(new RowView(gui, content, i, listSize.x));
        }
    }

    private static void BarPin(Button? button, float x)
    {
        if (button != null)
        {
            VanillaButtonFactory.Pin((RectTransform)button.transform, new Vector2(BarButtonWidth, BarHeight), new Vector2(x, -Margin));
        }
    }

    private static void Place(InventoryGui gui)
    {
        var parent = (RectTransform)gui.m_inventoryRoot;
        var screen = (RectTransform)parent.GetComponentInParent<Canvas>().rootCanvas.transform;
        screen.GetWorldCorners(Corners);
        Vector3 center = parent.InverseTransformPoint((Corners[0] + Corners[2]) * 0.5f);
        Rect bounds = parent.rect;
        float height = Mathf.Min(Height, screen.rect.height - ScreenMargin);
        root!.anchoredPosition = new Vector2(center.x - bounds.xMin - (Width * 0.5f), center.y - bounds.yMax + (height * 0.5f));
    }

    private static void Close()
    {
        root!.gameObject.SetActive(false);
        if (reopenBaseMenu)
        {
            reopenBaseMenu = false;
            BaseMenu.BaseMenuPanel.Reopen();
        }
    }

    private static void Show()
    {
        if (root == null || content == null)
        {
            return;
        }

        BaseEntry? from = entries.Find(e => e.Station == source);
        if (status != null)
        {
            status.text = loading
                ? StringTable.English.Get(StringKeys.BasesLoading)
                : entries.Count == 0
                    ? StringTable.English.Get(StringKeys.BasesNone)
                    : from == null
                        ? StringTable.English.Get(StringKeys.BasesPickSource)
                        : StringTable.English.Format(StringKeys.BasesMovingFrom, Plain(from.DisplayName));
        }

        content.sizeDelta = new Vector2(content.sizeDelta.x, Mathf.Max(Height, entries.Count * RowHeight));
        Player? player = Player.m_localPlayer;
        Vector3 at = player == null ? Vector3.zero : player.transform.position;
        ContainerId here = PluginBaseMove.StationHereId();
        for (int i = 0; i < Rows.Count; i++)
        {
            Rows[i].Show(i < entries.Count ? entries[i] : null, from, at, here);
        }
    }

    private static string Plain(string text) => text.Replace("<", "<noparse><</noparse>");

    private static TMP_Text? Label(InventoryGui gui, Transform parent, string id, Vector2 size, Vector2 position, float fontSize)
    {
        TMP_Text? template = gui.m_recipeDecription;
        if (template == null)
        {
            return null;
        }

        GameObject node = VanillaButtonFactory.CreateNode(parent, id, size, position, typeof(RectTransform), typeof(TextMeshProUGUI));
        TMP_Text text = node.GetComponent<TMP_Text>();
        text.font = template.font;
        text.fontSize = fontSize;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.richText = true;
        text.raycastTarget = false;
        return text;
    }

    private sealed class RowView
    {
        private readonly GameObject node;
        private readonly Image background;
        private readonly TMP_Text? name;
        private readonly TMP_Text? info;
        private readonly Button? from;
        private readonly Button? to;
        private BaseEntry? entry;

        internal RowView(InventoryGui gui, Transform parent, int index, float width)
        {
            node = VanillaButtonFactory.CreateNode(parent, RowId, new Vector2(width - 8f, RowHeight - 4f), new Vector2(4f, -index * RowHeight), typeof(RectTransform), typeof(Image));
            background = node.GetComponent<Image>();
            float textWidth = width - 8f - (3f * (RowButtonWidth + 6f)) - Margin;
            name = Label(gui, node.transform, NameId, new Vector2(textWidth, 30f), new Vector2(Margin * 0.5f, -2f), 22f);
            info = Label(gui, node.transform, InfoId, new Vector2(textWidth, 24f), new Vector2(Margin * 0.5f, -32f), 16f);
            float y = -((RowHeight - 4f - RowButtonHeight) * 0.5f);
            Pin(VanillaButtonFactory.Create(gui.m_stackAllButton, node.transform, RenameId, StringKeys.BasesRename, () => Click(OnRename)), textWidth + Margin, y);
            from = VanillaButtonFactory.Create(gui.m_stackAllButton, node.transform, FromId, StringKeys.BasesFrom, () => Click(OnFrom));
            Pin(from, textWidth + Margin + RowButtonWidth + 6f, y);
            to = VanillaButtonFactory.Create(gui.m_stackAllButton, node.transform, ToId, StringKeys.BasesTo, () => Click(OnTo));
            Pin(to, textWidth + Margin + (2f * (RowButtonWidth + 6f)), y);
            node.SetActive(false);
        }

        internal void Show(BaseEntry? shown, BaseEntry? source, Vector3 at, ContainerId here)
        {
            entry = shown;
            node.SetActive(shown != null);
            if (shown == null)
            {
                return;
            }

            bool isSource = source == shown;
            background.color = isSource ? Lit : Rest;
            if (name != null)
            {
                name.text = Plain(shown.DisplayName);
            }

            if (info != null)
            {
                string biome = shown.Biome.HasValue ? CompendiumBuilder.BiomeName(shown.Biome.Value) : StringTable.English.Get(StringKeys.BasesUnknownBiome);
                string where = Contains(shown, here)
                    ? StringTable.English.Get(StringKeys.BasesHere)
                    : StringTable.English.Format(StringKeys.BasesDistance, ((int)Mathf.Sqrt(BaseDirectory.DistanceSquaredTo(shown, at.x, at.z))).ToString(CultureInfo.InvariantCulture));
                info.text = StringTable.English.Format(StringKeys.BasesInfo, biome, shown.Chests, shown.FillPercent, where);
            }

            if (from != null)
            {
                from.interactable = !isSource;
            }

            if (to != null)
            {
                to.interactable = source != null && !isSource;
            }
        }

        private static void Pin(Button? button, float x, float y)
        {
            if (button != null)
            {
                VanillaButtonFactory.Pin((RectTransform)button.transform, new Vector2(RowButtonWidth, RowButtonHeight), new Vector2(x, y));
            }
        }

        private static void Click(Action action) => PatchSafety.Run(ClickKey, action);

        private void OnRename()
        {
            if (entry != null)
            {
                PluginBaseMove.AskName(entry.Station, entry.Name);
            }
        }

        private void OnFrom()
        {
            if (entry != null)
            {
                source = entry.Station;
                BasesWindow.Show();
            }
        }

        private void OnTo()
        {
            BaseEntry? sourceEntry = entries.Find(e => e.Station == source);
            if (entry != null && sourceEntry != null && sourceEntry != entry)
            {
                PluginBaseMove.StartMove(sourceEntry, entry);
            }
        }
    }
}
