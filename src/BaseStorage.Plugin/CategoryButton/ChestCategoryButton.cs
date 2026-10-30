namespace BaseStorage.Plugin.CategoryButton;

using System;
using BaseStorage.Core.Categories;
using BaseStorage.Core.CategoryButton;
using BaseStorage.Core.ChestCategories;
using BaseStorage.Core.Persistence;
using BaseStorage.Core.Snapshots;
using BaseStorage.Core.Ui;
using BaseStorage.Plugin.BaseMenu;
using BaseStorage.Plugin.ChestCategories;
using BaseStorage.Plugin.Patching;
using BaseStorage.Plugin.Ui;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The Category button of the open-chest panel (contract S-E004-004 section 4.1, DESIGN-SYSTEM component 3): a clone of the vanilla
/// Take All button, created once per InventoryGui instance from the UpdateContainer postfix, labelled with the open chest's current
/// value, disabled while a write for that chest is pending and shown exactly while a chest is open. Left click cycles forward, right
/// click (through <see cref="CategoryRightClickRelay"/>) cycles back; the writer shows the one notice. The per-frame refresh compares
/// the cached chest and stored text before touching any UI object, so a steady frame allocates nothing. Main thread only; Unity null
/// checks use <c>==</c> (W4).
/// </summary>
internal static class ChestCategoryButton
{
    /// <summary>The design-system control id; the clone is named with the BaseStorage prefix by the factory.</summary>
    internal const string ControlId = "chest.category.cycle-button";

    /// <summary>The patch key used by the fail-closed wrapper of the left-click handler.</summary>
    internal const string LeftClickKey = "BaseStorage.Plugin.CategoryButton.ChestCategoryButton.LeftClick";

    /// <summary>The patch key used by the fail-closed wrapper of the right-click handler.</summary>
    internal const string RightClickKey = "BaseStorage.Plugin.CategoryButton.ChestCategoryButton.RightClick";

    private static readonly GameChestCategoryStorage Storage = new GameChestCategoryStorage();

    private static InventoryGui? cachedGui;
    private static Button? button;
    private static Container? cachedChest;
    private static string? cachedRaw;

    /// <summary>Creates the button when needed and keeps its visibility, label and enabled state in step with the open chest.</summary>
    /// <param name="gui">The inventory GUI that vanilla just updated.</param>
    internal static void Refresh(InventoryGui gui)
    {
        BaseMenuPanel.Refresh(gui);
        if (gui == null)
        {
            return;
        }

        Button? control = EnsureButton(gui);
        if (control == null)
        {
            return;
        }

        Container? chest = gui.m_currentContainer;
        GameObject controlObject = control.gameObject;
        if (chest == null)
        {
            if (controlObject.activeSelf)
            {
                controlObject.SetActive(false);
            }

            cachedChest = null;
            cachedRaw = null;
            return;
        }

        if (!controlObject.activeSelf)
        {
            controlObject.SetActive(true);
        }

        string? raw = Storage.ReadRaw(chest);
        if (!ReferenceEquals(chest, cachedChest) || !string.Equals(raw, cachedRaw, StringComparison.Ordinal))
        {
            cachedChest = chest;
            cachedRaw = raw;
            VanillaButtonFactory.SetLabel(control, StringKeys.ChestCategoryButton, ChestCategoryReader.Label(ChestCategoryReader.Read(raw), StringTable.English));
        }

        bool interactable = !IsPending(chest);
        if (control.interactable != interactable)
        {
            control.interactable = interactable;
        }
    }

    /// <summary>Cycles the open chest back by one value. Called by the right-click relay.</summary>
    internal static void OnRightClick()
    {
        PatchSafety.Run(RightClickKey, static () => CycleOpenChest(forward: false));
    }

    /// <summary>Asks the writer for the next or previous value of a chest. Shared by the button and the hotkey.</summary>
    /// <param name="chest">The chest to cycle.</param>
    /// <param name="forward">True for the next value, false for the previous one.</param>
    internal static void Cycle(Container chest, bool forward)
    {
        if (chest == null)
        {
            return;
        }

        CategoryCatalog catalog = PluginCategoryCatalog.Current;
        CategoryAssignment? current = ChestCategoryWriter.Read(chest);
        CategoryAssignment? desired = forward ? CategoryCycle.Next(current, catalog) : CategoryCycle.Previous(current, catalog);
        ChestCategoryWriter.TrySet(chest, desired);
    }

    private static void OnLeftClick()
    {
        PatchSafety.Run(LeftClickKey, static () => OpenRules());
    }

    private static void OpenRules()
    {
        InventoryGui gui = InventoryGui.instance;
        if (gui != null && button != null && gui.m_currentContainer != null)
        {
            CategoryDropdown.Toggle(gui, gui.m_currentContainer, (RectTransform)button.transform);
        }
    }

    private static void CycleOpenChest(bool forward)
    {
        InventoryGui gui = InventoryGui.instance;
        if (gui == null)
        {
            return;
        }

        Cycle(gui.m_currentContainer, forward);
    }

    private static bool IsPending(Container chest)
    {
        ChestCategoryWriteDriver<Container>? driver = ChestCategoryWriter.Current;
        return driver != null && Storage.TryGetId(chest, out ContainerId id) && driver.IsPending(id);
    }

    /// <summary>Returns the cached clone for this GUI instance, or creates it beside the Take All button (CB1, CB2).</summary>
    private static Button? EnsureButton(InventoryGui gui)
    {
        Button? existing = button;
        if (ReferenceEquals(cachedGui, gui) && existing != null)
        {
            return existing;
        }

        Button template = gui.m_takeAllButton;
        if (template == null)
        {
            return null;
        }

        Button? created = VanillaButtonFactory.Create(template, template.transform.parent, ControlId, StringKeys.ChestCategoryAuto, OnLeftClick);
        if (created == null)
        {
            return null;
        }

        if (created.GetComponent<CategoryRightClickRelay>() == null)
        {
            created.gameObject.AddComponent<CategoryRightClickRelay>();
            Place(created, template, gui.m_stackAllButton);
        }

        cachedGui = gui;
        button = created;
        cachedChest = null;
        cachedRaw = null;
        return created;
    }

    /// <summary>Puts the clone on the Take All / Stack All row, outside the outermost sibling, with the design-system gap.</summary>
    private static void Place(Button clone, Button takeAll, Button stackAll)
    {
        RectTransform cloneRect = clone.GetComponent<RectTransform>();
        RectTransform takeAllRect = takeAll.GetComponent<RectTransform>();
        if (cloneRect == null || takeAllRect == null)
        {
            return;
        }

        RectTransform? stackAllRect = stackAll == null ? null : stackAll.GetComponent<RectTransform>();
        float stackAllX = stackAllRect == null ? float.NaN : stackAllRect.anchoredPosition.x;

        bool growsLeft = CategoryButtonLayout.GrowsLeft(takeAllRect.anchorMin.x);
        float outerX = CategoryButtonLayout.OuterX(takeAllRect.anchoredPosition.x, stackAllX, growsLeft);
        float width = Mathf.Max(takeAllRect.rect.width, cloneRect.rect.width);
        float x = CategoryButtonLayout.AnchoredX(outerX, width, growsLeft, CategoryButtonLayout.Gap);
        cloneRect.anchoredPosition = new Vector2(x, takeAllRect.anchoredPosition.y);
    }
}
