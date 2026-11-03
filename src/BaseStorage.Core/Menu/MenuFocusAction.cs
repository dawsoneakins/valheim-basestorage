namespace BaseStorage.Core.Menu;

/// <summary>What a gamepad press asks the panel to do.</summary>
public enum MenuFocusAction
{
    /// <summary>Nothing changed.</summary>
    None = 0,

    /// <summary>The focus moved.</summary>
    Moved = 1,

    /// <summary>Press what has the focus.</summary>
    Press = 2,

    /// <summary>Close the panel.</summary>
    Close = 3,
}
