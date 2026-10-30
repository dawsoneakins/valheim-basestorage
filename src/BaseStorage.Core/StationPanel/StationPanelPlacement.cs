namespace BaseStorage.Core.StationPanel;

/// <summary>Where the buttons are placed relative to the vanilla controls (contract 4.2). None means hidden.</summary>
public enum StationPanelPlacement
{
    /// <summary>Hidden, so no placement.</summary>
    None = 0,

    /// <summary>To the right of the craft button, on the craft button's row.</summary>
    RightOfCraftButton = 1,

    /// <summary>Below the craft button, with the row's top edge 8 units under the craft button.</summary>
    BelowCraftButton = 2,

    /// <summary>To the left of the craft button, on the craft button's row.</summary>
    LeftOfCraftButton = 3,

    /// <summary>Above the vanilla controls, under the top of the bounding box of the panel.</summary>
    AboveControls = 4,

    /// <summary>To the right of the vanilla controls, on the craft button's row.</summary>
    RightOfControls = 5,

    /// <summary>Below the craft button with the row's right edge on the craft button's right edge, for a panel in the screen corner.</summary>
    BelowCraftButtonRightAligned = 6,
}
