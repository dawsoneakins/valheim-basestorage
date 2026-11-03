namespace BaseStorage.Plugin.ChestSort;

using System;
using BaseStorage.Core.CategoryButton;
using BaseStorage.Core.Ui;
using BaseStorage.Plugin.ChestNames;
using BaseStorage.Plugin.Patching;
using BaseStorage.Plugin.Ui;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The Sort and Name buttons of the open-chest panel. The Category button's row is full, so these two sit under it, half its width each and
/// measured from it (rule R2). Main thread only.
/// </summary>
internal static class ChestPanelButtons
{
    private const string ClickKey = "BaseStorage.Plugin.ChestSort.ChestPanelButtons.Click";
    private const string SortId = "chest.sort-button";
    private const string NameId = "chest.name-button";

    private static Button? sort;
    private static Button? name;

    /// <summary>Creates the buttons under the Category button.</summary>
    /// <param name="template">The button to clone.</param>
    /// <param name="category">The Category button.</param>
    internal static void Build(Button template, Button category)
    {
        sort = VanillaButtonFactory.Create(template, template.transform.parent, SortId, StringKeys.ChestSort, OnSortClick);
        name = VanillaButtonFactory.Create(template, template.transform.parent, NameId, StringKeys.ChestName, OnNameClick);
        Place(sort, category, -1f);
        Place(name, category, 1f);
    }

    /// <summary>Shows or hides the buttons with the open chest.</summary>
    /// <param name="show">True while a chest is open.</param>
    internal static void Show(bool show)
    {
        if (sort != null && sort.gameObject.activeSelf != show)
        {
            sort.gameObject.SetActive(show);
        }

        if (name != null && name.gameObject.activeSelf != show)
        {
            name.gameObject.SetActive(show);
        }
    }

    /// <summary>Lists the buttons for the gamepad.</summary>
    /// <param name="add">Receives each control and what pressing it does.</param>
    internal static void CopyTargets(Action<Selectable?, Action> add)
    {
        add(sort, OnSortPressed);
        add(name, OnNamePressed);
    }

    private static void OnSortClick() => PatchSafety.Run(ClickKey, static () => OnSortPressed());

    private static void OnNameClick() => PatchSafety.Run(ClickKey, static () => OnNamePressed());

    private static void OnSortPressed()
    {
        InventoryGui gui = InventoryGui.instance;
        if (gui != null && gui.m_currentContainer != null)
        {
            ChestSorter.Start(gui.m_currentContainer);
        }
    }

    private static void OnNamePressed()
    {
        InventoryGui gui = InventoryGui.instance;
        if (gui != null && gui.m_currentContainer != null)
        {
            BaseStorage.Plugin.ChestNames.ChestNames.Ask(gui.m_currentContainer);
        }
    }

    private static void Place(Button? button, Button category, float side)
    {
        RectTransform? rect = button == null ? null : button.GetComponent<RectTransform>();
        RectTransform anchor = category.GetComponent<RectTransform>();
        if (rect == null || anchor == null)
        {
            return;
        }

        float width = (anchor.rect.width - CategoryButtonLayout.Gap) / 2f;
        float height = anchor.rect.height;
        rect.sizeDelta = new Vector2(width, height);
        float offset = (width + CategoryButtonLayout.Gap) / 2f;
        rect.anchoredPosition = anchor.anchoredPosition + new Vector2(side * offset, -(height + CategoryButtonLayout.Gap));
    }
}
