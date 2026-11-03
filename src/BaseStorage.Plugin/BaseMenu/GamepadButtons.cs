namespace BaseStorage.Plugin.BaseMenu;

/// <summary>The game's logical gamepad button names, read with ZInput. The game maps them to the player's layout.</summary>
internal static class GamepadButtons
{
    /// <summary>Takes the focus: the right stick press, which the inventory screen does not use.</summary>
    internal const string FocusButton = "JoyRStick";

    /// <summary>Confirm.</summary>
    internal const string Confirm = "JoyButtonA";

    /// <summary>D-pad up.</summary>
    internal const string Up = "JoyDPadUp";

    /// <summary>D-pad down.</summary>
    internal const string Down = "JoyDPadDown";

    /// <summary>D-pad left.</summary>
    internal const string Left = "JoyDPadLeft";

    /// <summary>D-pad right.</summary>
    internal const string Right = "JoyDPadRight";

    /// <summary>Left stick up.</summary>
    internal const string StickUp = "JoyLStickUp";

    /// <summary>Left stick down.</summary>
    internal const string StickDown = "JoyLStickDown";

    /// <summary>Left stick left.</summary>
    internal const string StickLeft = "JoyLStickLeft";

    /// <summary>Left stick right.</summary>
    internal const string StickRight = "JoyLStickRight";
}
