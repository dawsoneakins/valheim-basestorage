namespace BaseStorage.Plugin.StationActions;

using System;
using System.Collections.Generic;
using BaseStorage.Core.Abstractions;
using BaseStorage.Core.Bases.Membership;
using BaseStorage.Core.Bases.Resolution;
using BaseStorage.Core.Locking;
using BaseStorage.Core.Logging;
using BaseStorage.Core.Organize.Run;
using BaseStorage.Core.Persistence;
using BaseStorage.Core.Settings;
using BaseStorage.Core.Snapshots;
using BaseStorage.Core.StationPanel;
using BaseStorage.Core.Ui;
using BaseStorage.Plugin.ChestCategories;
using BaseStorage.Plugin.Deposit;
using BaseStorage.Plugin.Organize;
using BaseStorage.Plugin.Patching;
using BaseStorage.Plugin.Ui;
using BaseStorage.Plugin.World;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static BaseStorage.Core.Logging.LogPolicy;

/// <summary>
/// The Organize base button and the base status line of the crafting panel (contract S-E004-012 section 3.5). Core decides everything
/// the player sees (StationPanelLayout); this class reads the vanilla rects and the base facts, builds the input, and applies the
/// returned state exactly. The controls are clones made once per InventoryGui instance, and a steady frame allocates nothing. Main
/// thread only; Unity null checks use <c>==</c> (W4).
/// </summary>
internal static class StationActionsPanel
{
    /// <summary>The design-system id of the button; the clone is named with the BaseStorage prefix by the factory.</summary>
    internal const string OrganizeButtonId = "station.actions.organize-button";

    /// <summary>The design-system id of the status line.</summary>
    internal const string StatusLineId = "station.actions.status-line";

    /// <summary>The design-system id of the Put away inventory button.</summary>
    internal const string DepositButtonId = "station.actions.deposit-button";

    /// <summary>The patch key used by the fail-closed wrapper of the click handler.</summary>
    internal const string ClickKey = "BaseStorage.Plugin.StationActions.StationActionsPanel.OrganizeClick";

    /// <summary>The patch key used by the fail-closed wrapper of the Put away inventory click handler.</summary>
    internal const string DepositClickKey = "BaseStorage.Plugin.StationActions.StationActionsPanel.DepositClick";

    private static readonly Vector3[] Corners = new Vector3[4];

    private static PluginWorld? world;
    private static IClock? clock;
    private static BaseStorageSettings? settings;
    private static ILogSink? sink;
    private static IOrganizeLockStore? lockStore;
    private static StationActionsMemo? memo;

    private static InventoryGui? createdFor;
    private static InventoryGui? rootFor;
    private static Canvas? rootCanvasCache;
    private static RectTransform? rootRectCache;
    private static Button? button;
    private static TMP_Text? label;
    private static UITooltip? tooltip;
    private static Button? depositButton;
    private static UITooltip? depositTooltip;

    private static bool hasStation;
    private static StationId stationId;

    private static bool factButtons;
    private static int factCount;
    private static int factEligible;
    private static int factFill;
    private static string? factHolder;

    private static int inWidth;
    private static int inHeight;
    private static float inScale;
    private static PanelRect inList;
    private static PanelRect inRequirements;
    private static PanelRect inCraft;
    private static int inCount;
    private static int inEligible;
    private static int inFill;
    private static string? inHolder;
    private static bool inButtons;

    private static StationPanelState? currentState;
    private static StationPanelState? appliedState;
    private static bool noRoomLogged;

    /// <summary>Stores the services and creates the lock store and the memo. Later calls change nothing.</summary>
    /// <param name="world">The plugin world.</param>
    /// <param name="clock">The clock.</param>
    /// <param name="settings">The settings.</param>
    /// <param name="sink">The log sink of the Debug lines.</param>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static void Initialize(PluginWorld world, IClock clock, BaseStorageSettings settings, ILogSink sink)
    {
        if (world == null)
        {
            throw new ArgumentNullException(nameof(world));
        }

        if (clock == null)
        {
            throw new ArgumentNullException(nameof(clock));
        }

        if (settings == null)
        {
            throw new ArgumentNullException(nameof(settings));
        }

        if (sink == null)
        {
            throw new ArgumentNullException(nameof(sink));
        }

        if (memo != null)
        {
            return;
        }

        StationActionsPanel.world = world;
        StationActionsPanel.clock = clock;
        StationActionsPanel.settings = settings;
        StationActionsPanel.sink = sink;
        lockStore = new GameOrganizeLockStore(world);
        memo = new StationActionsMemo();
    }

    /// <summary>Creates the controls when needed and keeps them in step with the station in use. Called from the UpdateRecipe postfix.</summary>
    /// <param name="gui">The inventory GUI that vanilla just updated.</param>
    internal static void Refresh(InventoryGui gui)
    {
        StationActionsMemo? facts = memo;
        IClock? time = clock;
        BaseStorageSettings? config = settings;
        PluginWorld? current = PluginWorld.Current;
        if (gui == null || facts == null || time == null || config == null)
        {
            return;
        }

        if (current == null || !TryGetStation(out StationId id))
        {
            HideControls();
            hasStation = false;
            appliedState = null;
            return;
        }

        stationId = id;
        hasStation = true;
        TimeSpan now = time.Elapsed;
        if (facts.FactsDue(id, now))
        {
            ReadFacts(current, config, id, now);
            facts.MarkRead(id, now);
        }

        Button? craft = gui.m_craftButton;
        if (craft == null)
        {
            return;
        }

        RectTransform? craftRect = craft.transform as RectTransform;
        if (craftRect == null || !ResolveRoot(gui, craft))
        {
            return;
        }

        Canvas rootCanvas = rootCanvasCache!;
        RectTransform rootRect = rootRectCache!;
        int width = Screen.width;
        int height = Screen.height;
        if (width < 1 || height < 1)
        {
            return;
        }

        float scale = StationPanelGeometry.UiScaleOf(width, height, rootCanvas.scaleFactor);
        PanelRect craftBox = RectOf(craftRect, rootRect);
        PanelRect listBox = RectOf(ListRect(gui), rootRect);
        PanelRect requirementsBox = RequirementsBox(gui, rootRect);
        UpdateState(width, height, scale, listBox, requirementsBox, craftBox);

        if (!ReferenceEquals(createdFor, gui))
        {
            appliedState = null;
        }

        StationPanelState? state = currentState;
        if (state == null || state == appliedState)
        {
            return;
        }

        appliedState = state;
        if (state.Visible)
        {
            ShowState(gui, state, rootRect);
        }
        else
        {
            HideControls();
            LogNoRoom(state);
        }
    }

    /// <summary>
    /// Resolves the root canvas and its rect (contract SA7) once per InventoryGui instance, and again only when the cached
    /// canvas is destroyed. A steady frame takes the cached path and allocates nothing. Unity null checks use <c>==</c> (W4).
    /// </summary>
    private static bool ResolveRoot(InventoryGui gui, Button craft)
    {
        if (ReferenceEquals(rootFor, gui) && rootCanvasCache != null && rootRectCache != null)
        {
            return true;
        }

        rootFor = null;
        rootCanvasCache = null;
        rootRectCache = null;
        Canvas? parent = craft.GetComponentInParent<Canvas>();
        if (parent == null)
        {
            return false;
        }

        Canvas root = parent.rootCanvas;
        RectTransform? rect = root == null ? null : root.transform as RectTransform;
        if (root == null || rect == null)
        {
            return false;
        }

        rootFor = gui;
        rootCanvasCache = root;
        rootRectCache = rect;
        return true;
    }

    /// <summary>The click handler of the Organize base button.</summary>
    internal static void OnOrganizeClick()
    {
        PatchSafety.Run(ClickKey, static () => Click());
    }

    private static void Click()
    {
        StationActionsMemo? facts = memo;
        if (!hasStation || facts == null)
        {
            return;
        }

        OrganizeStartOutcome outcome = OrganizeRunner.StartAt(BaseStart.FromStation(stationId));
        facts.Invalidate();
        if (IsEnabled(sink, LogCategory.Detail))
        {
            Write(sink, LogCategory.Detail, StationActionsMessages.Clicked(outcome));
        }
    }

    /// <summary>The click handler of the Put away inventory button.</summary>
    internal static void OnDepositClick()
    {
        PatchSafety.Run(DepositClickKey, static () => DepositClick());
    }

    private static void DepositClick()
    {
        StationActionsMemo? facts = memo;
        if (!hasStation || facts == null)
        {
            return;
        }

        OrganizeStartOutcome outcome = OrganizeRunner.StartDepositAt(BaseStart.FromStation(stationId));
        facts.Invalidate();
        if (IsEnabled(sink, LogCategory.Detail))
        {
            Write(sink, LogCategory.Detail, DepositMessages.Clicked(outcome));
        }
    }

    private static bool TryGetStation(out StationId id)
    {
        id = default;
        Player player = Player.m_localPlayer;
        if (player == null)
        {
            return false;
        }

        CraftingStation station = player.GetCurrentCraftingStation();
        if (station == null || station.m_nview == null || !station.m_nview.IsValid())
        {
            return false;
        }

        ZDO zdo = station.m_nview.GetZDO();
        if (zdo == null)
        {
            return false;
        }

        ZDOID uid = zdo.m_uid;
        id = new StationId(uid.UserID, uid.ID);
        return true;
    }

    /// <summary>The once-a-second step that reads the base facts. It may allocate; nothing else on the frame path does.</summary>
    private static void ReadFacts(PluginWorld current, BaseStorageSettings config, StationId id, TimeSpan now)
    {
        BaseResolution resolution = current.Cache.Resolve(
            BaseStart.FromStation(id),
            new BaseCacheOptions(config.BaseCacheSeconds, config.MaxBaseStations));

        factButtons = false; // v0.2: Organize and Put away live only in the Base Storage menu.
        factCount = StationActionsFacts.StationCount(resolution);

        var loaded = new List<Container>();
        current.Containers.CopyItemsTo(loaded);
        var byId = new Dictionary<ContainerId, Container>();
        for (int i = 0; i < loaded.Count; i++)
        {
            Container container = loaded[i];
            if (container == null || container.m_nview == null || !container.m_nview.IsValid())
            {
                continue;
            }

            ZDO zdo = container.m_nview.GetZDO();
            if (zdo != null)
            {
                ZDOID uid = zdo.m_uid;
                byId[new ContainerId(uid.UserID, uid.ID)] = container;
            }
        }

        var assignments = new Dictionary<ContainerId, CategoryAssignment>();
        for (int i = 0; i < resolution.Containers.Count; i++)
        {
            ContainerId member = resolution.Containers[i].Id;
            if (byId.TryGetValue(member, out Container? found))
            {
                CategoryAssignment? assignment = ChestCategoryWriter.Read(found);
                if (assignment != null)
                {
                    assignments[member] = assignment;
                }
            }
        }

        EligibilityOptions options = EligibilityOptions.FromSettings(config);
        factEligible = StationActionsFacts.EligibleChestCount(resolution, assignments, options);
        factFill = StationActionsFacts.FillPercent(resolution, assignments, options);
        IOrganizeLockStore? store = lockStore;
        factHolder = store == null ? null : StationActionsFacts.HolderName(resolution.Stations, store, now, config.OrganizeLockSeconds);
    }

    private static RectTransform? ListRect(InventoryGui gui)
    {
        RectTransform? root = gui.m_recipeListRoot;
        if (root == null)
        {
            return null;
        }

        RectTransform? viewport = root.parent as RectTransform;
        return viewport == null ? root : viewport;
    }

    private static PanelRect RequirementsBox(InventoryGui gui, RectTransform rootRect)
    {
        GameObject[]? list = gui.m_recipeRequirementList;
        PanelRect box = default;
        if (list == null)
        {
            return box;
        }

        for (int i = 0; i < list.Length; i++)
        {
            GameObject element = list[i];
            if (element != null)
            {
                box = StationPanelGeometry.Union(box, RectOf(element.transform as RectTransform, rootRect));
            }
        }

        return box;
    }

    private static PanelRect RectOf(RectTransform? element, RectTransform rootRect)
    {
        if (element == null)
        {
            return default;
        }

        element.GetWorldCorners(Corners);
        Vector3 low = rootRect.InverseTransformPoint(Corners[0]);
        Vector3 high = rootRect.InverseTransformPoint(Corners[2]);
        Rect bounds = rootRect.rect;
        return StationPanelGeometry.FromCorners(low.x - bounds.xMin, low.y - bounds.yMin, high.x - bounds.xMin, high.y - bounds.yMin);
    }

    private static void UpdateState(int width, int height, float scale, PanelRect list, PanelRect requirements, PanelRect craft)
    {
        if (currentState != null
            && width == inWidth
            && height == inHeight
            && scale == inScale
            && list == inList
            && requirements == inRequirements
            && craft == inCraft
            && factCount == inCount
            && factEligible == inEligible
            && factFill == inFill
            && factButtons == inButtons
            && string.Equals(factHolder, inHolder, StringComparison.Ordinal))
        {
            return;
        }

        inWidth = width;
        inHeight = height;
        inScale = scale;
        inList = list;
        inRequirements = requirements;
        inCraft = craft;
        inCount = factCount;
        inEligible = factEligible;
        inFill = factFill;
        inButtons = factButtons;
        inHolder = factHolder;
        currentState = StationPanelLayout.Compute(
            new StationPanelInput(width, height, scale, list, requirements, craft, factCount, factEligible, factFill, factHolder, factButtons));
    }

    private static void HideControls()
    {
        HideButton(button);
        HideButton(depositButton);

        TMP_Text? line = label;
        if (line != null)
        {
            GameObject lineObject = line.gameObject;
            if (lineObject.activeSelf)
            {
                lineObject.SetActive(false);
            }
        }
    }

    private static void HideButton(Button? control)
    {
        if (control == null)
        {
            return;
        }

        GameObject controlObject = control.gameObject;
        if (controlObject.activeSelf)
        {
            controlObject.SetActive(false);
        }
    }

    private static void ShowButton(Button control, UITooltip? hover, PanelRect rect, StationPanelState state, RectTransform rootRect)
    {
        GameObject controlObject = control.gameObject;
        if (!controlObject.activeSelf)
        {
            controlObject.SetActive(true);
        }

        Place(control.transform as RectTransform, rect, rootRect);
        bool enabled = state.ButtonsEnabled;
        if (control.interactable != enabled)
        {
            control.interactable = enabled;
        }

        string tip = state.TooltipText ?? string.Empty;
        if (hover != null && !string.Equals(hover.m_text, tip, StringComparison.Ordinal))
        {
            hover.m_text = tip;
        }
    }

    private static void ShowState(InventoryGui gui, StationPanelState state, RectTransform rootRect)
    {
        if (!EnsureControls(gui))
        {
            return;
        }

        Button? control = button;
        if (control == null)
        {
            return;
        }

        ShowButton(control, tooltip, state.OrganizeButton, state, rootRect);
        Button? deposit = depositButton;
        if (deposit != null)
        {
            ShowButton(deposit, depositTooltip, state.DepositButton, state, rootRect);
        }

        TMP_Text? line = label;
        if (line == null)
        {
            return;
        }

        GameObject lineObject = line.gameObject;
        if (!lineObject.activeSelf)
        {
            lineObject.SetActive(true);
        }

        Place(line.transform as RectTransform, state.StatusLine, rootRect);
        string text = state.StatusLineText ?? string.Empty;
        if (!string.Equals(line.text, text, StringComparison.Ordinal))
        {
            line.text = text;
        }
    }

    private static void Place(RectTransform? target, PanelRect rect, RectTransform rootRect)
    {
        if (target == null)
        {
            return;
        }

        target.anchorMin = Vector2.zero;
        target.anchorMax = Vector2.zero;
        target.pivot = Vector2.zero;
        Vector3 rootScale = rootRect.lossyScale;
        Vector3 ownScale = target.lossyScale;
        target.sizeDelta = new Vector2(
            StationPanelGeometry.ToLocalSize(rect.Width, rootScale.x, ownScale.x),
            StationPanelGeometry.ToLocalSize(rect.Height, rootScale.y, ownScale.y));
        Rect bounds = rootRect.rect;
        target.position = rootRect.TransformPoint(new Vector3(rect.X + bounds.xMin, rect.Y + bounds.yMin, 0f));
    }

    private static void LogNoRoom(StationPanelState state)
    {
        if (noRoomLogged || state.HiddenReason != StationPanelHiddenReason.NoRoom || !IsEnabled(sink, LogCategory.Detail))
        {
            return;
        }

        noRoomLogged = true;
        Write(sink, LogCategory.Detail, StationActionsMessages.NoRoom);
    }

    /// <summary>Creates the clones once per InventoryGui instance, on the first visible state. Returns whether the button exists.</summary>
    private static bool EnsureControls(InventoryGui gui)
    {
        if (ReferenceEquals(createdFor, gui))
        {
            return button != null;
        }

        createdFor = gui;
        button = null;
        label = null;
        tooltip = null;
        depositButton = null;
        depositTooltip = null;
        RectTransform parent = gui.m_crafting;
        if (parent == null)
        {
            return false;
        }

        Button? created = VanillaButtonFactory.Create(
            gui.m_craftButton,
            gui.m_crafting,
            OrganizeButtonId,
            StringKeys.StationActionsOrganizeButton,
            OnOrganizeClick);
        if (created == null)
        {
            return false;
        }

        button = created;
        tooltip = created.GetComponent<UITooltip>();
        PrepareClone(created.gameObject);

        Button? deposit = VanillaButtonFactory.Create(
            gui.m_craftButton,
            gui.m_crafting,
            DepositButtonId,
            StringKeys.StationActionsDepositButton,
            OnDepositClick);
        if (deposit != null)
        {
            depositButton = deposit;
            depositTooltip = deposit.GetComponent<UITooltip>();
            PrepareClone(deposit.gameObject);
        }

        TMP_Text? line = VanillaButtonFactory.CreateText(gui.m_craftingStationName, gui.m_crafting, StatusLineId);
        if (line != null)
        {
            label = line;
            PrepareClone(line.gameObject);
            line.raycastTarget = false;
            line.textWrappingMode = TextWrappingModes.NoWrap;
            line.overflowMode = TextOverflowModes.Ellipsis;
            line.alignment = TextAlignmentOptions.MidlineLeft;
        }

        return true;
    }

    /// <summary>Stops the cloned craft binding from pressing the clone and keeps the clone out of the vanilla layout group.</summary>
    private static void PrepareClone(GameObject clone)
    {
        UIGamePad[] pads = clone.GetComponentsInChildren<UIGamePad>(true);
        for (int i = 0; i < pads.Length; i++)
        {
            UIGamePad pad = pads[i];
            pad.enabled = false;
            GameObject hint = pad.m_hint;
            if (hint != null && hint.activeSelf)
            {
                hint.SetActive(false);
            }
        }

        LayoutElement? element = clone.GetComponent<LayoutElement>();
        if (element == null)
        {
            element = clone.AddComponent<LayoutElement>();
            element.ignoreLayout = true;
        }
    }
}
