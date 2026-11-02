namespace BaseStorage.Plugin.Favourites;

using System;
using BaseStorage.Core.Persistence;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The FavouriteMarker refresher (contract S-E004-014 section 7, DESIGN-SYSTEM component 9). It runs every frame from the UpdateGui
/// postfix for the player grid only, keeps one marker per slot element, and allocates nothing in steady state: markers are created
/// lazily, found before they are created, and the per-grid array is reallocated only when the grid or its size changes.
/// </summary>
internal static class FavouriteMarkers
{
    private const string MarkerName = "BaseStorage.inventory.slot.favourite-marker";
    private const float MarkerSize = 22f;
    private const float BrandPrimaryR = 1f;
    private const float BrandPrimaryG = 0.6470588f;
    private const float BrandPrimaryB = 0f;

    private static InventoryGrid? cachedGrid;
    private static Image?[] markers = Array.Empty<Image?>();

    /// <summary>Shows the marker on every favourite item of the player grid and hides it everywhere else.</summary>
    /// <param name="grid">The grid that vanilla just updated.</param>
    internal static void Refresh(InventoryGrid grid)
    {
        InventoryGui? gui = InventoryGui.instance;
        if (gui == null || !ReferenceEquals(grid, gui.m_playerGrid))
        {
            return;
        }

        FavouriteItemStore? favourites = FavouriteSession.ItemsForLocalPlayer();
        if (favourites == null)
        {
            HideAll();
            return;
        }

        Inventory? inventory = grid.GetInventory();
        if (inventory == null)
        {
            return;
        }

        int width = inventory.GetWidth();
        int height = inventory.GetHeight();
        if (width <= 0 || height <= 0)
        {
            return;
        }

        if (!ReferenceEquals(cachedGrid, grid) || markers.Length != width * height)
        {
            cachedGrid = grid;
            markers = new Image?[width * height];
        }

        FavouriteItems current = favourites.Current;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                InventoryElement? element = grid.GetElement(x, y, width);
                if (element == null)
                {
                    continue;
                }

                ItemDrop.ItemData? item = inventory.GetItemAt(x, y);
                bool want = item != null && current.Contains(item.m_shared.m_name);
                int index = (y * width) + x;
                Image? marker = markers[index];
                if (marker == null)
                {
                    if (!want)
                    {
                        continue;
                    }

                    marker = FindOrCreate(element);
                    markers[index] = marker;
                    if (marker == null)
                    {
                        continue;
                    }
                }

                GameObject markerObject = marker.gameObject;
                if (markerObject.activeSelf != want)
                {
                    markerObject.SetActive(want);
                }
            }
        }
    }

    private static void HideAll()
    {
        for (int i = 0; i < markers.Length; i++)
        {
            Image? marker = markers[i];
            if (marker != null && marker.gameObject.activeSelf)
            {
                marker.gameObject.SetActive(false);
            }
        }
    }

    /// <summary>Finds the element's marker under the icon's parent, or clones the element's own queued image into one (FM2 to FM5).</summary>
    private static Image? FindOrCreate(InventoryElement element)
    {
        if (element.m_icon == null || element.m_queued == null)
        {
            return null;
        }

        Transform? parent = element.m_icon.transform.parent;
        if (parent == null)
        {
            return null;
        }

        Transform? existing = parent.Find(MarkerName);
        if (existing != null)
        {
            return existing.GetComponent<Image>();
        }

        Image clone = UnityEngine.Object.Instantiate(element.m_queued, parent);
        clone.name = MarkerName;
        clone.enabled = true; // Vanilla keeps the queued image off unless an equip is queued, and the clone copies that.
        clone.sprite = FavouriteStar.Sprite;
        clone.type = Image.Type.Simple;
        clone.preserveAspect = true;
        clone.color = new Color(BrandPrimaryR, BrandPrimaryG, BrandPrimaryB, 1f);
        clone.raycastTarget = false;

        RectTransform rect = clone.rectTransform;
        rect.anchorMin = new Vector2(1f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(1f, 0f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(MarkerSize, MarkerSize);
        rect.localScale = Vector3.one;

        clone.transform.SetAsLastSibling(); // On top: item icons fill the slot and would hide a marker behind them.
        return clone;
    }
}
