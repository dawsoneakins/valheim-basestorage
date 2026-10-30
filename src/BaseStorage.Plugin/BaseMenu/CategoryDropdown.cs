namespace BaseStorage.Plugin.BaseMenu;

using System;
using System.Collections.Generic;
using BaseStorage.Core.ChestCategories;
using BaseStorage.Core.Persistence;
using BaseStorage.Core.Ui;
using BaseStorage.Plugin.CategoryButton;
using BaseStorage.Plugin.ChestCategories;
using BaseStorage.Plugin.Patching;
using BaseStorage.Plugin.Ui;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The list of chest rules, shared by the chest window's Category button and the menu's chest rows. It opens under its control, or
/// above it near the bottom of the screen. One is built per InventoryGui and reused. Main thread only.
/// </summary>
internal static class CategoryDropdown
{
    private const string ClickKey = "BaseStorage.Plugin.BaseMenu.CategoryDropdown.Click";
    private const string RootId = "menu.dropdown";
    private const string OptionId = "menu.dropdown.option";
    private const string LabelId = "menu.dropdown.label";
    private const float Width = 190f;
    private const float RowHeight = 28f;
    private const float FontSize = 16f;
    private const float Pad = 3f;

    private static readonly List<GameObject> Rows = new List<GameObject>();
    private static readonly List<TMP_Text?> Labels = new List<TMP_Text?>();
    private static readonly Vector3[] Corners = new Vector3[4];

    private static InventoryGui? builtFor;
    private static GameObject? root;
    private static IReadOnlyList<CategoryAssignment?> order = Array.Empty<CategoryAssignment?>();
    private static Container? chest;

    /// <summary>Opens the list for a chest under a control, or closes it if it is already open for that chest.</summary>
    /// <param name="gui">The inventory GUI.</param>
    /// <param name="target">The chest.</param>
    /// <param name="anchor">The control to open beside.</param>
    internal static void Toggle(InventoryGui gui, Container target, RectTransform anchor)
    {
        if (root != null && root.activeSelf && ReferenceEquals(chest, target))
        {
            Close();
            return;
        }

        Build(gui);
        chest = target;
        order = CategoryCycle.Order(PluginCategoryCatalog.Current);
        Fill(gui);
        Place(anchor);
        root!.transform.SetAsLastSibling();
        root.SetActive(true);
    }

    /// <summary>Closes the list.</summary>
    internal static void Close()
    {
        if (root != null)
        {
            root.SetActive(false);
        }
    }

    private static void Build(InventoryGui gui)
    {
        if (ReferenceEquals(builtFor, gui) && root != null)
        {
            return;
        }

        builtFor = gui;
        Rows.Clear();
        Labels.Clear();
        root = VanillaButtonFactory.CreateNode(gui.m_inventoryRoot, RootId, new Vector2(Width, RowHeight), Vector2.zero, typeof(RectTransform), typeof(Image));
        root.GetComponent<Image>().color = new Color(0.1f, 0.075f, 0.05f, 1f);
        root.AddComponent<LayoutElement>().ignoreLayout = true;
    }

    private static void Fill(InventoryGui gui)
    {
        for (int i = Rows.Count; i < order.Count; i++)
        {
            int index = i;
            GameObject row = VanillaButtonFactory.CreateNode(root!.transform, OptionId, new Vector2(Width - (2 * Pad), RowHeight - 2f), new Vector2(Pad, -Pad - (i * RowHeight)), typeof(RectTransform), typeof(Image), typeof(Button));
            row.GetComponent<Image>().color = new Color(1f, 0.8f, 0.4f, 0.1f);
            row.GetComponent<Button>().onClick.AddListener(() => PatchSafety.Run(ClickKey, () => Choose(index)));
            TMP_Text? label = VanillaButtonFactory.CreateText(gui.m_craftingStationName, row.transform, LabelId);
            if (label != null)
            {
                VanillaButtonFactory.Pin((RectTransform)label.transform, new Vector2(Width - 16f, RowHeight - 2f), new Vector2(6f, 0f));
                label.alignment = TextAlignmentOptions.MidlineLeft;
                label.enableAutoSizing = false;
                label.fontSize = FontSize;
                label.raycastTarget = false;
                label.gameObject.SetActive(true);
            }

            Rows.Add(row);
            Labels.Add(label);
        }

        for (int i = 0; i < Rows.Count; i++)
        {
            Rows[i].SetActive(i < order.Count);
            TMP_Text? label = Labels[i];
            if (label != null && i < order.Count)
            {
                label.text = ChestCategoryReader.Label(order[i], StringTable.English);
            }
        }

        ((RectTransform)root!.transform).sizeDelta = new Vector2(Width, (order.Count * RowHeight) + (2 * Pad));
    }

    private static void Place(RectTransform anchor)
    {
        var rect = (RectTransform)root!.transform;
        anchor.GetWorldCorners(Corners);
        float height = rect.sizeDelta.y * rect.lossyScale.y;
        bool up = Corners[0].y - height < 0f;
        rect.pivot = up ? new Vector2(0f, 0f) : new Vector2(0f, 1f);
        rect.position = up ? Corners[1] : Corners[0];
    }

    private static void Choose(int index)
    {
        Container? target = chest;
        Close();
        if (target != null && index < order.Count)
        {
            ChestCategoryWriter.TrySet(target, order[index]);
        }
    }
}
