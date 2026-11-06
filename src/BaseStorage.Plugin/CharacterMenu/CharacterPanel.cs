namespace BaseStorage.Plugin.CharacterMenu;

using System;
using System.Collections.Generic;
using BaseStorage.Core.CharacterMenu;
using BaseStorage.Core.Ui;
using BaseStorage.Plugin.Patching;
using BaseStorage.Plugin.Ui;
using UnityEngine;
using UnityEngine.UI;

/// <summary>The panel below the inventory: worn slots, hand slots and the 3D view. It hides while a chest is open. Main thread only.</summary>
internal static class CharacterPanel
{
    /// <summary>The patch key for the panel's tick.</summary>
    internal const string TickKey = "BaseStorage.Plugin.CharacterMenu.CharacterPanel.Tick";

    private const string ClickKey = "BaseStorage.Plugin.CharacterMenu.CharacterPanel.Click";
    private const string PanelId = "character.panel";
    private const string BindingChild = "binding";
    private const string CompendiumButtonId = "character.compendium";
    private const string DividerId = "character.divider";
    private const float CompendiumButtonHeight = 30f;
    private const float Margin = 14f;
    private const float DividerSpace = 12f;
    private const float DividerWidth = 2f;
    private const float HandSlotAlpha = 0.6f;
    private const float Gap = 6f;
    private const float MinViewWidth = 110f;
    private const float RefreshSeconds = 0.25f;
    private const int Rows = 3;

    private static readonly WornSlot[] LeftColumn = { WornSlot.Helmet, WornSlot.Chest, WornSlot.Legs };
    private static readonly WornSlot[] RightColumn = { WornSlot.Cape, WornSlot.Utility, WornSlot.Trinket };
    private static readonly Vector3[] Corners = new Vector3[4];
    private static readonly Color GhostTint = new Color(0f, 0f, 0f, 0.4f);
    private static readonly HashSet<string> GhostPrefabs = new HashSet<string>(StringComparer.Ordinal)
    {
        "HelmetLeather", "ArmorLeatherChest", "ArmorLeatherLegs", "CapeDeerHide", "BeltStrength", "Club", "ShieldWood", "ArrowWood",
    };

    private static readonly Dictionary<ItemDrop.ItemData.ItemType, Sprite> Ghosts = new Dictionary<ItemDrop.ItemData.ItemType, Sprite>();

    private static RectTransform? panel;
    private static InventoryElement?[] worn = new InventoryElement?[WornSlots.Count];
    private static InventoryElement? weapon;
    private static InventoryElement? offHand;
    private static InventoryElement? ammo;
    private static float nextRefresh;

    /// <summary>Shows, hides and refreshes the panel.</summary>
    internal static void Tick() => PatchSafety.Run(TickKey, TickBody);

    private static void TickBody()
    {
        InventoryGui? gui = InventoryGui.instance;
        Player? player = Player.m_localPlayer;
        if (gui == null || player == null || !InventoryGui.IsVisible() || gui.m_currentContainer != null || Compendium.CompendiumWindow.IsOpen || BaseMove.BasesWindow.IsOpen)
        {
            if (panel != null)
            {
                panel.gameObject.SetActive(false);
            }

            CharacterPreview.Pause();
            return;
        }

        if (panel == null)
        {
            Build(gui);
            nextRefresh = 0f;
        }

        panel!.gameObject.SetActive(true);
        if (Time.unscaledTime < nextRefresh)
        {
            return;
        }

        nextRefresh = Time.unscaledTime + RefreshSeconds;
        Place(gui);
        WornItems.ReturnStranded(player);
        Refresh(gui, player);
        CharacterPreview.Refresh(player);
    }

    private static void Build(InventoryGui gui)
    {
        float pitch = gui.m_playerGrid.m_elementSpace;
        float width = LocalWidth(gui, (RectTransform)gui.m_inventoryRoot);
        float slot = ((RectTransform)gui.m_playerGrid.m_elementPrefab.transform).sizeDelta.y;
        float viewHeight = ((Rows - 1) * pitch) + slot;
        float height = (2f * Margin) + viewHeight + Gap + CompendiumButtonHeight;
        GameObject node = VanillaButtonFactory.CreateNode(gui.m_inventoryRoot, PanelId, new Vector2(width, height), Vector2.zero, typeof(RectTransform), typeof(Image));
        PanelStyle.Wood(node.GetComponent<Image>(), gui);
        panel = (RectTransform)node.transform;
        LoadGhosts();

        float viewWidth = Mathf.Max(MinViewWidth, width - (2f * Margin) - (3f * pitch) - DividerSpace - Gap);
        float viewX = Margin + pitch;
        float rightX = viewX + viewWidth + Gap;
        float handX = rightX + pitch + DividerSpace;
        for (int row = 0; row < Rows; row++)
        {
            float y = -Margin - (row * pitch);
            worn[(int)LeftColumn[row]] = WornElement(gui, LeftColumn[row], new Vector2(Margin, y));
            worn[(int)RightColumn[row]] = WornElement(gui, RightColumn[row], new Vector2(rightX, y));
        }

        weapon = HandElement(gui, new Vector2(handX, -Margin));
        offHand = HandElement(gui, new Vector2(handX, -Margin - pitch));
        ammo = HandElement(gui, new Vector2(handX, -Margin - (2f * pitch)));
        GameObject divider = VanillaButtonFactory.CreateNode(panel, DividerId, new Vector2(DividerWidth, viewHeight), new Vector2(handX - ((DividerSpace + pitch - slot + DividerWidth) * 0.5f), -Margin), typeof(RectTransform), typeof(Image));
        divider.GetComponent<Image>().color = PanelStyle.Divider;
        CharacterPreview.BuildImage(panel, new Vector2(viewWidth, viewHeight), new Vector2(viewX, -Margin));
        float buttonY = -Margin - viewHeight - Gap;
        Button? book = VanillaButtonFactory.Create(gui.m_stackAllButton, panel, CompendiumButtonId, StringKeys.CompendiumButton, static () => PatchSafety.Run(ClickKey, Compendium.CompendiumWindow.Open));
        if (book != null)
        {
            VanillaButtonFactory.Pin((RectTransform)book.transform, new Vector2(width - (2f * Margin), CompendiumButtonHeight), new Vector2(Margin, buttonY));
        }
    }

    /// <summary>Picks one plain icon per slot type to darken for empty slots. Any other item of the type stands in if a named one is missing.</summary>
    private static void LoadGhosts()
    {
        Ghosts.Clear();
        foreach (GameObject prefab in ObjectDB.instance.m_items)
        {
            ItemDrop? drop = prefab == null ? null : prefab.GetComponent<ItemDrop>();
            ItemDrop.ItemData.SharedData? shared = drop == null ? null : drop.m_itemData.m_shared;
            if (shared?.m_icons != null && shared.m_icons.Length > 0 && (GhostPrefabs.Contains(prefab!.name) || !Ghosts.ContainsKey(shared.m_itemType)))
            {
                Ghosts[shared.m_itemType] = shared.m_icons[0];
            }
        }
    }

    private static Sprite? GhostOf(ItemDrop.ItemData.ItemType type) => Ghosts.TryGetValue(type, out Sprite sprite) ? sprite : null;

    private static ItemDrop.ItemData.ItemType TypeOf(WornSlot slot)
    {
        switch (slot)
        {
            case WornSlot.Helmet:
                return ItemDrop.ItemData.ItemType.Helmet;
            case WornSlot.Chest:
                return ItemDrop.ItemData.ItemType.Chest;
            case WornSlot.Legs:
                return ItemDrop.ItemData.ItemType.Legs;
            case WornSlot.Cape:
                return ItemDrop.ItemData.ItemType.Shoulder;
            case WornSlot.Utility:
                return ItemDrop.ItemData.ItemType.Utility;
            default:
                return ItemDrop.ItemData.ItemType.Trinket;
        }
    }

    private static InventoryElement WornElement(InventoryGui gui, WornSlot slot, Vector2 position)
    {
        InventoryElement element = Element(gui, position);
        UIInputHandler handler = element.GetComponentInChildren<UIInputHandler>();
        handler.m_onLeftDown += _ => PatchSafety.Run(ClickKey, () => OnWornClick(gui, slot));
        handler.m_onRightDown += _ => PatchSafety.Run(ClickKey, () => OnWornClick(gui, slot));
        return element;
    }

    /// <summary>A hand slot only mirrors what is equipped, so its background is fainter than a slot that takes clicks.</summary>
    private static InventoryElement HandElement(InventoryGui gui, Vector2 position)
    {
        InventoryElement element = Element(gui, position);
        Image? background = element.GetComponent<Image>();
        if (background != null)
        {
            Color color = background.color;
            color.a *= HandSlotAlpha;
            background.color = color;
        }

        return element;
    }

    private static InventoryElement Element(InventoryGui gui, Vector2 position)
    {
        GameObject node = VanillaButtonFactory.CopyElement(gui.m_playerGrid.m_elementPrefab, panel!);
        var rect = (RectTransform)node.transform;
        VanillaButtonFactory.Pin(rect, rect.sizeDelta, position);

        // The prefab's hotbar number has no meaning on a character slot.
        Transform binding = node.transform.Find(BindingChild);
        if (binding != null)
        {
            binding.gameObject.SetActive(false);
        }

        return node.GetComponent<InventoryElement>();
    }

    private static void OnWornClick(InventoryGui gui, WornSlot slot)
    {
        Player? player = Player.m_localPlayer;
        if (player == null)
        {
            return;
        }

        ItemDrop.ItemData? held = gui.m_dragItem;
        if (held != null)
        {
            if (gui.m_dragInventory != player.GetInventory() || WornItems.SlotOf(held) != slot)
            {
                Notices.Show(NoticePlacement.TopLeft, StringKeys.MessageWornWrongSlot);
                return;
            }

            if (player.EquipItem(held))
            {
                gui.SetupDragItem(null, null, 1);
            }
        }
        else
        {
            ItemDrop.ItemData? item = WornItems.InSlot(player, slot);
            if (item != null)
            {
                WornItems.TakeOff(player, item);
            }
        }

        nextRefresh = 0f;
    }

    private static void Refresh(InventoryGui gui, Player player)
    {
        for (int i = 0; i < worn.Length; i++)
        {
            var slot = (WornSlot)i;
            bool target = gui.m_dragItem != null && gui.m_dragInventory == player.GetInventory() && WornItems.SlotOf(gui.m_dragItem) == slot;
            Show(gui, worn[i], WornItems.InSlot(player, slot), LabelOf(slot), GhostOf(TypeOf(slot)), target);
        }

        ItemDrop.ItemData? right = player.GetRightItem();
        ItemDrop.ItemData? left = player.GetLeftItem();
        ItemDrop.ItemData.ItemType? leftType = left?.m_shared?.m_itemType;
        bool leftIsOffHand = leftType == ItemDrop.ItemData.ItemType.Shield || leftType == ItemDrop.ItemData.ItemType.Torch;
        HandSource weaponHand = HandSlots.Weapon(right != null, left != null, leftIsOffHand);
        HandSource offHandSource = HandSlots.OffHand(left != null, leftIsOffHand);
        Show(gui, weapon, weaponHand == HandSource.Right ? right : (weaponHand == HandSource.Left ? left : null), StringKeys.CharacterSlotWeapon, GhostOf(ItemDrop.ItemData.ItemType.OneHandedWeapon), false);
        Show(gui, offHand, offHandSource == HandSource.Left ? left : null, StringKeys.CharacterSlotShield, GhostOf(ItemDrop.ItemData.ItemType.Shield), false);
        Show(gui, ammo, player.GetAmmoItem(), StringKeys.CharacterSlotAmmo, GhostOf(ItemDrop.ItemData.ItemType.Ammo), false);
    }

    private static string LabelOf(WornSlot slot)
    {
        switch (slot)
        {
            case WornSlot.Helmet:
                return StringKeys.CharacterSlotHelmet;
            case WornSlot.Chest:
                return StringKeys.CharacterSlotChest;
            case WornSlot.Legs:
                return StringKeys.CharacterSlotLegs;
            case WornSlot.Cape:
                return StringKeys.CharacterSlotCape;
            case WornSlot.Utility:
                return StringKeys.CharacterSlotUtility;
            default:
                return StringKeys.CharacterSlotTrinket;
        }
    }

    private static void Show(InventoryGui gui, InventoryElement? element, ItemDrop.ItemData? item, string labelKey, Sprite? ghost, bool target)
    {
        if (element == null)
        {
            return;
        }

        string label = StringTable.English.Get(labelKey);
        element.m_icon.enabled = item != null || ghost != null;
        if (item != null)
        {
            element.m_icon.sprite = item.GetIcon();
            element.m_icon.color = item.m_equipped ? Color.white : Color.grey;
        }
        else if (ghost != null)
        {
            element.m_icon.sprite = ghost;
            element.m_icon.color = GhostTint;
        }

        // The label stays as the fallback for a slot with no outline, and always shows in the tooltip.
        bool showLabel = item == null && ghost == null;
        element.m_amount.enabled = showLabel;
        element.m_amount.text = showLabel ? label : string.Empty;
        // The game fills an equipped item's slot with a bright highlight. Here that would be every slot, so an unequipped item's grey icon marks the difference.
        element.m_equiped.enabled = false;
        element.m_quality.enabled = item != null && item.m_shared.m_maxQuality > 1;
        element.m_quality.text = item == null ? string.Empty : item.m_quality.ToString(System.Globalization.CultureInfo.InvariantCulture);
        element.m_queued.enabled = false;
        element.m_noteleport.enabled = false;
        element.m_food.enabled = false;
        element.m_selected.SetActive(target);
        ShowDurability(element, item);
        if (item == null)
        {
            element.m_tooltip.Set(label, string.Empty);
        }
        else
        {
            gui.m_playerGrid.CreateItemTooltip(item, element.m_tooltip);
        }
    }

    private static void ShowDurability(InventoryElement element, ItemDrop.ItemData? item)
    {
        bool shows = item != null && item.m_shared.m_useDurability && item.m_durability < item.GetMaxDurability();
        element.m_durability.gameObject.SetActive(shows);
        if (shows)
        {
            element.m_durability.ResetColor();
            element.m_durability.SetValue(item!.GetDurabilityPercentage());
        }
    }

    private static void Place(InventoryGui gui)
    {
        var parent = (RectTransform)gui.m_inventoryRoot;
        gui.m_player.GetWorldCorners(Corners);
        Vector3 bottomLeft = parent.InverseTransformPoint(Corners[0]);
        Rect bounds = parent.rect;
        panel!.anchoredPosition = new Vector2(bottomLeft.x - bounds.xMin, bottomLeft.y - bounds.yMax - Gap);
    }

    private static float LocalWidth(InventoryGui gui, RectTransform parent)
    {
        gui.m_player.GetWorldCorners(Corners);
        return parent.InverseTransformPoint(Corners[2]).x - parent.InverseTransformPoint(Corners[0]).x;
    }
}
