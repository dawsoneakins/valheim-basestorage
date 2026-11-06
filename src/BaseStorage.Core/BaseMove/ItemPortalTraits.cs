namespace BaseStorage.Core.BaseMove;

/// <summary>The two item fields the game's portal check reads. The Plugin copies them from the item's shared data.</summary>
public readonly struct ItemPortalTraits
{
    /// <summary>Creates the traits of one item type.</summary>
    /// <param name="toolTier">The item's <c>m_toolTier</c>.</param>
    /// <param name="teleportable">The item's <c>m_teleportable</c>.</param>
    public ItemPortalTraits(int toolTier, bool teleportable)
    {
        ToolTier = toolTier;
        Teleportable = teleportable;
    }

    /// <summary>Gets the item's tool tier.</summary>
    public int ToolTier { get; }

    /// <summary>Gets a value indicating whether the item may go through a portal on a world with default rules.</summary>
    public bool Teleportable { get; }
}
