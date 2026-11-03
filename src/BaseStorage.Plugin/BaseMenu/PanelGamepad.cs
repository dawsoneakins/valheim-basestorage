namespace BaseStorage.Plugin.BaseMenu;

using System;
using System.Collections.Generic;
using BaseStorage.Core.Menu;
using BaseStorage.Core.Ui;
using BaseStorage.Plugin.CategoryButton;
using BaseStorage.Plugin.Feeding;
using BaseStorage.Plugin.Patching;
using BaseStorage.Plugin.Ui;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Gamepad control of the mod's panels. The right stick press opens the base menu, or takes the focus on the open chest's buttons. While a
/// panel has the focus the game's own inventory navigation waits, and the keyboard and mouse are untouched. Main thread only.
/// </summary>
internal static class PanelGamepad
{
    private const string PressKey = "BaseStorage.Plugin.BaseMenu.PanelGamepad.Press";
    private static readonly Color Highlight = new Color(1f, 0.8f, 0.3f, 1f);
    private static readonly Vector2 HighlightWidth = new Vector2(3f, -3f);
    private static readonly MenuFocus Focus = new MenuFocus();
    private static readonly List<Target> Buttons = new List<Target>();
    private static readonly List<Target> Rows = new List<Target>();

    private static bool chestFocus;
    private static Outline? marked;
    private static object? shownPanel;

    /// <summary>Gets a value indicating whether a panel has the gamepad, so the game's inventory navigation must wait.</summary>
    internal static bool Active { get; private set; }

    /// <summary>Follows the gamepad. Called once per frame while the inventory is shown.</summary>
    /// <param name="gui">The inventory GUI.</param>
    internal static void Tick(InventoryGui gui)
    {
        if (!ZInput.IsGamepadActive() || TextInput.IsVisible() || UnifiedPopup.IsVisible())
        {
            Leave();
            return;
        }

        Buttons.Clear();
        Rows.Clear();
        int columns = Collect(gui, out bool search, out int rowCells, out object? panelNow);
        Active = panelNow != null;
        if (ZInput.GetButtonDown(GamepadButtons.FocusButton))
        {
            OnFocusButton(gui);
            return;
        }

        if (!Active)
        {
            Mark(null);
            shownPanel = null;
            return;
        }

        Focus.Set(Buttons.Count, columns, search, Rows.Count / rowCells, rowCells);
        if (!ReferenceEquals(panelNow, shownPanel))
        {
            shownPanel = panelNow;
            Focus.Reset();
            Notices.Show(NoticePlacement.TopLeft, StringKeys.MenuGamepadHint);
        }

        Read();
        Mark(Current());
    }

    /// <summary>Gives the focus back to the game.</summary>
    internal static void Leave()
    {
        Active = false;
        chestFocus = false;
        shownPanel = null;
        Mark(null);
    }

    private static void OnFocusButton(InventoryGui gui)
    {
        if (FeedPanel.IsOpen || BaseMenuPanel.IsOpen)
        {
            return;
        }

        if (gui.m_currentContainer != null && !chestFocus)
        {
            chestFocus = true;
            return;
        }

        chestFocus = false;
        BaseMenuPanel.ToggleFromGamepad();
    }

    private static int Collect(InventoryGui gui, out bool search, out int rowCells, out object? panel)
    {
        search = false;
        rowCells = 1;
        if (FeedPanel.IsOpen)
        {
            chestFocus = false;
            panel = typeof(FeedPanel);
            return FeedPanel.CopyTargets(Add);
        }

        if (BaseMenuPanel.IsOpen)
        {
            chestFocus = false;
            panel = typeof(BaseMenuPanel);
            search = true;
            rowCells = 2;
            BaseMenuPanel.CopyTargets(Add, AddRow);
            return BaseMenuPanel.ButtonColumns;
        }

        if (gui.m_currentContainer == null)
        {
            chestFocus = false;
        }

        if (chestFocus)
        {
            panel = typeof(ChestCategoryButton);
            ChestCategoryButton.CopyTargets(Add);
            return Math.Max(1, Buttons.Count);
        }

        panel = null;
        return 1;
    }

    private static void Add(Selectable? control, Action press)
    {
        if (control != null)
        {
            Buttons.Add(new Target(control, press));
        }
    }

    private static void AddRow(Selectable? control, Action press) => Rows.Add(new Target(control, press));

    private static void Read()
    {
        MenuFocusAction action = MenuFocusAction.None;
        if (Pressed(GamepadButtons.Up, GamepadButtons.StickUp))
        {
            action = Focus.Apply(MenuFocusInput.Up);
        }
        else if (Pressed(GamepadButtons.Down, GamepadButtons.StickDown))
        {
            action = Focus.Apply(MenuFocusInput.Down);
        }
        else if (Pressed(GamepadButtons.Left, GamepadButtons.StickLeft))
        {
            action = Focus.Apply(MenuFocusInput.Left);
        }
        else if (Pressed(GamepadButtons.Right, GamepadButtons.StickRight))
        {
            action = Focus.Apply(MenuFocusInput.Right);
        }
        else if (ZInput.GetButtonDown(GamepadButtons.Confirm))
        {
            action = Focus.Apply(MenuFocusInput.Confirm);
        }

        if (action == MenuFocusAction.Press)
        {
            Press();
        }
        else if (action == MenuFocusAction.Moved && Focus.Zone == MenuFocusZone.Rows)
        {
            BaseMenuPanel.ScrollToRow(Focus.Index);
        }
    }

    private static bool Pressed(string pad, string stick) => ZInput.GetButtonDown(pad) || ZInput.GetButtonDown(stick);

    private static void Press()
    {
        if (Focus.Zone == MenuFocusZone.Search)
        {
            PatchSafety.Run(PressKey, static () => BaseMenuPanel.AskSearchText());
            return;
        }

        Target? target = Current();
        if (target.HasValue)
        {
            PatchSafety.Run(PressKey, target.Value.Press);
        }
    }

    private static Target? Current()
    {
        if (Focus.Zone == MenuFocusZone.Search)
        {
            return BaseMenuPanel.SearchTarget == null ? null : new Target(BaseMenuPanel.SearchTarget, static () => { });
        }

        if (Focus.Zone == MenuFocusZone.Rows)
        {
            int cell = (Focus.Index * 2) + Focus.Column;
            return cell < Rows.Count ? Rows[cell] : null;
        }

        return Focus.Index < Buttons.Count ? Buttons[Focus.Index] : null;
    }

    private static void Mark(Target? target)
    {
        Graphic? graphic = target?.Control == null ? null : target.Value.Control!.targetGraphic;
        Outline? wanted = null;
        if (graphic != null)
        {
            wanted = graphic.GetComponent<Outline>();
            if (wanted == null)
            {
                wanted = graphic.gameObject.AddComponent<Outline>();
                wanted.effectColor = Highlight;
                wanted.effectDistance = HighlightWidth;
                wanted.enabled = false;
            }
        }

        if (ReferenceEquals(marked, wanted))
        {
            return;
        }

        if (marked != null)
        {
            marked.enabled = false;
        }

        marked = wanted;
        if (marked != null)
        {
            marked.enabled = true;
        }
    }

    private readonly struct Target
    {
        internal Target(Selectable? control, Action press)
        {
            Control = control;
            Press = press;
        }

        internal Selectable? Control { get; }

        internal Action Press { get; }
    }
}
