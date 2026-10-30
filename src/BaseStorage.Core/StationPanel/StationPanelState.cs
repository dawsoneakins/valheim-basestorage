namespace BaseStorage.Core.StationPanel;

using System;

/// <summary>
/// The computed state of the station buttons and the status line (contract 4.0). Instances come only from
/// <see cref="StationPanelLayout.Compute"/>. Equality covers every property. A hidden state has default rectangles and null texts.
/// </summary>
public sealed class StationPanelState : IEquatable<StationPanelState>
{
    private readonly bool visible;
    private readonly StationPanelHiddenReason hiddenReason;
    private readonly StationPanelPlacement placement;
    private readonly float canvasWidth;
    private readonly float canvasHeight;
    private readonly PanelRect organizeButton;
    private readonly PanelRect depositButton;
    private readonly PanelRect statusLine;
    private readonly bool buttonsEnabled;
    private readonly StationPanelDisabledReason disabledReason;
    private readonly string? tooltipKey;
    private readonly string? tooltipText;
    private readonly string? statusLineText;

    internal StationPanelState(
        bool visible,
        StationPanelHiddenReason hiddenReason,
        StationPanelPlacement placement,
        float canvasWidth,
        float canvasHeight,
        PanelRect organizeButton,
        PanelRect depositButton,
        PanelRect statusLine,
        bool buttonsEnabled,
        StationPanelDisabledReason disabledReason,
        string? tooltipKey,
        string? tooltipText,
        string? statusLineText)
    {
        this.visible = visible;
        this.hiddenReason = hiddenReason;
        this.placement = placement;
        this.canvasWidth = canvasWidth;
        this.canvasHeight = canvasHeight;
        this.organizeButton = organizeButton;
        this.depositButton = depositButton;
        this.statusLine = statusLine;
        this.buttonsEnabled = buttonsEnabled;
        this.disabledReason = disabledReason;
        this.tooltipKey = tooltipKey;
        this.tooltipText = tooltipText;
        this.statusLineText = statusLineText;
    }

    /// <summary>Gets a value indicating whether the buttons and the status line are shown.</summary>
    public bool Visible => visible;

    /// <summary>Gets why the buttons are hidden. None when they are shown.</summary>
    public StationPanelHiddenReason HiddenReason => hiddenReason;

    /// <summary>Gets the placement of the buttons relative to the vanilla controls. None when hidden.</summary>
    public StationPanelPlacement Placement => placement;

    /// <summary>Gets the canvas width in root-canvas units. It is always computed, even when hidden.</summary>
    public float CanvasWidth => canvasWidth;

    /// <summary>Gets the canvas height in root-canvas units. It is always computed, even when hidden.</summary>
    public float CanvasHeight => canvasHeight;

    /// <summary>Gets the rectangle of the Organize base button. It is the default rectangle when hidden.</summary>
    public PanelRect OrganizeButton => organizeButton;

    /// <summary>Gets the rectangle of the Put away inventory button. It is the default rectangle when hidden.</summary>
    public PanelRect DepositButton => depositButton;

    /// <summary>Gets the rectangle of the status line. It is the default rectangle when hidden.</summary>
    public PanelRect StatusLine => statusLine;

    /// <summary>Gets a value indicating whether both buttons are enabled. It is false when hidden.</summary>
    public bool ButtonsEnabled => buttonsEnabled;

    /// <summary>Gets why both buttons are disabled. None when enabled or hidden.</summary>
    public StationPanelDisabledReason DisabledReason => disabledReason;

    /// <summary>Gets the string-table key of the tooltip. It is null unless the state is visible and disabled.</summary>
    public string? TooltipKey => tooltipKey;

    /// <summary>Gets the tooltip text. It is null unless the state is visible and disabled.</summary>
    public string? TooltipText => tooltipText;

    /// <summary>Gets the status line text, with its colour tags. It is null when hidden.</summary>
    public string? StatusLineText => statusLineText;

    /// <summary>Compares two states for equality. Two nulls are equal.</summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns>True when the states are equal.</returns>
    public static bool operator ==(StationPanelState? left, StationPanelState? right) => left is null ? right is null : left.Equals(right);

    /// <summary>Compares two states for inequality.</summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns>True when the states differ.</returns>
    public static bool operator !=(StationPanelState? left, StationPanelState? right) => !(left == right);

    /// <summary>Compares every property with another state.</summary>
    /// <param name="other">The state to compare with.</param>
    /// <returns>True when every property is equal.</returns>
    public bool Equals(StationPanelState? other) =>
        other is not null
        && visible == other.visible
        && hiddenReason == other.hiddenReason
        && placement == other.placement
        && canvasWidth == other.canvasWidth
        && canvasHeight == other.canvasHeight
        && organizeButton == other.organizeButton
        && depositButton == other.depositButton
        && statusLine == other.statusLine
        && buttonsEnabled == other.buttonsEnabled
        && disabledReason == other.disabledReason
        && string.Equals(tooltipKey, other.tooltipKey, StringComparison.Ordinal)
        && string.Equals(tooltipText, other.tooltipText, StringComparison.Ordinal)
        && string.Equals(statusLineText, other.statusLineText, StringComparison.Ordinal);

    /// <summary>Compares this state with an object.</summary>
    /// <param name="obj">The object to compare with.</param>
    /// <returns>True when the object is an equal StationPanelState.</returns>
    public override bool Equals(object? obj) => Equals(obj as StationPanelState);

    /// <summary>Gets a hash code that is equal for equal states.</summary>
    /// <returns>The hash code.</returns>
    public override int GetHashCode()
    {
        unchecked
        {
            int h = visible ? 1 : 0;
            h = (h * 31) + (int)hiddenReason;
            h = (h * 31) + (int)placement;
            h = (h * 31) + canvasWidth.GetHashCode();
            h = (h * 31) + canvasHeight.GetHashCode();
            h = (h * 31) + organizeButton.GetHashCode();
            h = (h * 31) + depositButton.GetHashCode();
            h = (h * 31) + statusLine.GetHashCode();
            h = (h * 31) + (buttonsEnabled ? 1 : 0);
            h = (h * 31) + (int)disabledReason;
            h = (h * 31) + (tooltipKey == null ? 0 : StringComparer.Ordinal.GetHashCode(tooltipKey));
            h = (h * 31) + (tooltipText == null ? 0 : StringComparer.Ordinal.GetHashCode(tooltipText));
            h = (h * 31) + (statusLineText == null ? 0 : StringComparer.Ordinal.GetHashCode(statusLineText));
            return h;
        }
    }
}
