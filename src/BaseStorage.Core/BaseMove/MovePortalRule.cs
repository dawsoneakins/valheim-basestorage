namespace BaseStorage.Core.BaseMove;

/// <summary>
/// Decides whether an item may move between bases, item by item, the way the game's <c>Inventory.IsTeleportable</c> decides for a
/// whole inventory: a tool tier of 1000 or more never passes, the <c>TeleportAll</c> world key lets everything else pass, and without
/// that key the item's own flag decides. Pure.
/// </summary>
public static class MovePortalRule
{
    /// <summary>The tool tier from which the game blocks an item whatever the world setting is.</summary>
    public const int NeverMovesToolTier = 1000;

    /// <summary>Decides one item type.</summary>
    /// <param name="traits">The item's portal fields.</param>
    /// <param name="teleportAll">True when the world has the <c>TeleportAll</c> key.</param>
    /// <returns>True when the item may move.</returns>
    public static bool CanMove(ItemPortalTraits traits, bool teleportAll)
    {
        if (traits.ToolTier >= NeverMovesToolTier)
        {
            return false;
        }

        return teleportAll || traits.Teleportable;
    }
}
