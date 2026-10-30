namespace BaseStorage.Core.StationPanel;

/// <summary>Why the station buttons and the status line are hidden (contract 4.1). None means they are shown.</summary>
public enum StationPanelHiddenReason
{
    /// <summary>The buttons are shown.</summary>
    None = 0,

    /// <summary>The StationButtons setting is off.</summary>
    StationButtonsOff = 1,

    /// <summary>The station is not in a base (StationCount is 0).</summary>
    NotInBase = 2,

    /// <summary>No candidate placement fits the canvas without covering a vanilla control (SP6).</summary>
    NoRoom = 3,
}
