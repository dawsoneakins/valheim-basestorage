namespace BaseStorage.Core.StationPanel;

using System;

/// <summary>
/// The plain data that <see cref="StationPanelLayout.Compute"/> reads (contract section 3). Core never reads settings, locks or bases:
/// the caller passes the values. Equality covers every stored value, and OrganizeRunning is derived.
/// </summary>
public sealed class StationPanelInput : IEquatable<StationPanelInput>
{
    private readonly int screenWidth;
    private readonly int screenHeight;
    private readonly float uiScale;
    private readonly PanelRect recipeList;
    private readonly PanelRect requirements;
    private readonly PanelRect craftButton;
    private readonly int stationCount;
    private readonly int eligibleChestCount;
    private readonly int fillPercent;
    private readonly string? organizeHolderName;
    private readonly bool stationButtons;

    /// <summary>Creates the input. Validation runs in parameter order, and the first failure throws.</summary>
    /// <param name="screenWidth">The screen width in pixels. Must be at least 1.</param>
    /// <param name="screenHeight">The screen height in pixels. Must be at least 1.</param>
    /// <param name="uiScale">The player's UI scale. Must be finite and greater than 0.</param>
    /// <param name="recipeList">The vanilla recipe list rectangle, in canvas units.</param>
    /// <param name="requirements">The vanilla requirements rectangle, in canvas units.</param>
    /// <param name="craftButton">The vanilla craft button rectangle, in canvas units.</param>
    /// <param name="stationCount">The stations in this station's base. Must not be negative. 0 means the station is not in a base.</param>
    /// <param name="eligibleChestCount">The eligible chests in the base. Must not be negative.</param>
    /// <param name="fillPercent">The share of occupied slots in the eligible chests, from 0 to 100.</param>
    /// <param name="organizeHolderName">The name of the holder of a running Organize or Deposit, or null when none runs. Must not be blank.</param>
    /// <param name="stationButtons">The StationButtons setting.</param>
    public StationPanelInput(
        int screenWidth,
        int screenHeight,
        float uiScale,
        PanelRect recipeList,
        PanelRect requirements,
        PanelRect craftButton,
        int stationCount,
        int eligibleChestCount,
        int fillPercent,
        string? organizeHolderName,
        bool stationButtons)
    {
        if (screenWidth < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(screenWidth), screenWidth, "Screen width must be at least 1.");
        }

        if (screenHeight < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(screenHeight), screenHeight, "Screen height must be at least 1.");
        }

        if (float.IsNaN(uiScale) || float.IsInfinity(uiScale) || uiScale <= 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(uiScale), uiScale, "UI scale must be finite and greater than 0.");
        }

        if (stationCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(stationCount), stationCount, "Station count must not be negative.");
        }

        if (eligibleChestCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(eligibleChestCount), eligibleChestCount, "Eligible chest count must not be negative.");
        }

        if (fillPercent < 0 || fillPercent > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(fillPercent), fillPercent, "Fill percent must be from 0 to 100.");
        }

        if (organizeHolderName != null && string.IsNullOrWhiteSpace(organizeHolderName))
        {
            throw new ArgumentException("Holder name must not be blank.", nameof(organizeHolderName));
        }

        this.screenWidth = screenWidth;
        this.screenHeight = screenHeight;
        this.uiScale = uiScale;
        this.recipeList = recipeList;
        this.requirements = requirements;
        this.craftButton = craftButton;
        this.stationCount = stationCount;
        this.eligibleChestCount = eligibleChestCount;
        this.fillPercent = fillPercent;
        this.organizeHolderName = organizeHolderName;
        this.stationButtons = stationButtons;
    }

    /// <summary>Gets the screen width in pixels.</summary>
    public int ScreenWidth => screenWidth;

    /// <summary>Gets the screen height in pixels.</summary>
    public int ScreenHeight => screenHeight;

    /// <summary>Gets the player's UI scale setting.</summary>
    public float UiScale => uiScale;

    /// <summary>Gets the vanilla recipe list rectangle, in canvas units.</summary>
    public PanelRect RecipeList => recipeList;

    /// <summary>Gets the vanilla requirements rectangle, in canvas units.</summary>
    public PanelRect Requirements => requirements;

    /// <summary>Gets the vanilla craft button rectangle, in canvas units.</summary>
    public PanelRect CraftButton => craftButton;

    /// <summary>Gets the stations in this station's base. 0 means the station is not in a base.</summary>
    public int StationCount => stationCount;

    /// <summary>Gets the eligible chests in the base.</summary>
    public int EligibleChestCount => eligibleChestCount;

    /// <summary>Gets the share of occupied slots in the eligible chests, from 0 to 100.</summary>
    public int FillPercent => fillPercent;

    /// <summary>Gets the holder name of a running Organize or Deposit, or null when none runs.</summary>
    public string? OrganizeHolderName => organizeHolderName;

    /// <summary>Gets a value indicating whether an Organize or a Deposit runs in this base. It is true when OrganizeHolderName is not null.</summary>
    public bool OrganizeRunning => organizeHolderName != null;

    /// <summary>Gets the StationButtons setting.</summary>
    public bool StationButtons => stationButtons;

    /// <summary>Compares two inputs for equality. Two nulls are equal.</summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns>True when the inputs are equal.</returns>
    public static bool operator ==(StationPanelInput? left, StationPanelInput? right) => left is null ? right is null : left.Equals(right);

    /// <summary>Compares two inputs for inequality.</summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns>True when the inputs differ.</returns>
    public static bool operator !=(StationPanelInput? left, StationPanelInput? right) => !(left == right);

    /// <summary>Compares every stored value with another input.</summary>
    /// <param name="other">The input to compare with.</param>
    /// <returns>True when every stored value is equal.</returns>
    public bool Equals(StationPanelInput? other) =>
        other is not null
        && screenWidth == other.screenWidth
        && screenHeight == other.screenHeight
        && uiScale == other.uiScale
        && recipeList == other.recipeList
        && requirements == other.requirements
        && craftButton == other.craftButton
        && stationCount == other.stationCount
        && eligibleChestCount == other.eligibleChestCount
        && fillPercent == other.fillPercent
        && string.Equals(organizeHolderName, other.organizeHolderName, StringComparison.Ordinal)
        && stationButtons == other.stationButtons;

    /// <summary>Compares this input with an object.</summary>
    /// <param name="obj">The object to compare with.</param>
    /// <returns>True when the object is an equal StationPanelInput.</returns>
    public override bool Equals(object? obj) => Equals(obj as StationPanelInput);

    /// <summary>Gets a hash code that is equal for equal inputs.</summary>
    /// <returns>The hash code.</returns>
    public override int GetHashCode()
    {
        unchecked
        {
            int h = screenWidth;
            h = (h * 31) + screenHeight;
            h = (h * 31) + uiScale.GetHashCode();
            h = (h * 31) + recipeList.GetHashCode();
            h = (h * 31) + requirements.GetHashCode();
            h = (h * 31) + craftButton.GetHashCode();
            h = (h * 31) + stationCount;
            h = (h * 31) + eligibleChestCount;
            h = (h * 31) + fillPercent;
            h = (h * 31) + (organizeHolderName == null ? 0 : StringComparer.Ordinal.GetHashCode(organizeHolderName));
            h = (h * 31) + (stationButtons ? 1 : 0);
            return h;
        }
    }
}
