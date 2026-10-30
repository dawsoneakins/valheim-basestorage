namespace BaseStorage.Core.Tests.StationPanel;

using System;
using System.Globalization;
using BaseStorage.Core.StationPanel;

/// <summary>
/// The contract 5.1 fixtures, the literal expected rectangles and texts, the In helper and WithCulture (ADR-GWIN-041). These are static
/// properties or methods, never static readonly fields, so a throwing stub cannot cause a TypeInitializationException. Every expected
/// value is a literal copied from the contract. None is computed from StationPanelLayout, StringTable or Plural.
/// </summary>
internal static class StationPanelFixtures
{
    /// <summary>Fixture A recipe list, canvas units.</summary>
    public static PanelRect ARecipes => new PanelRect(40f, 220f, 300f, 560f);

    /// <summary>Fixture A requirements row, canvas units.</summary>
    public static PanelRect AReqs => new PanelRect(360f, 310f, 420f, 80f);

    /// <summary>Fixture A craft button, canvas units.</summary>
    public static PanelRect ACraft => new PanelRect(600f, 244f, 180f, 46f);

    /// <summary>Fixture B recipe list, canvas units.</summary>
    public static PanelRect BRecipes => new PanelRect(760f, 220f, 300f, 560f);

    /// <summary>Fixture B requirements row, canvas units.</summary>
    public static PanelRect BReqs => new PanelRect(1080f, 310f, 420f, 80f);

    /// <summary>Fixture B craft button, canvas units.</summary>
    public static PanelRect BCraft => new PanelRect(1120f, 244f, 180f, 46f);

    /// <summary>A-right: the Organize button of contract 5.1.</summary>
    public static PanelRect ARightOrganize => new PanelRect(788f, 244f, 180f, 46f);

    /// <summary>A-right: the Deposit button of contract 5.1.</summary>
    public static PanelRect ARightDeposit => new PanelRect(976f, 244f, 180f, 46f);

    /// <summary>A-right: the status line of contract 5.1.</summary>
    public static PanelRect ARightStatus => new PanelRect(788f, 204f, 368f, 24f);

    /// <summary>B-right: the Organize button of contract 5.1.</summary>
    public static PanelRect BRightOrganize => new PanelRect(1308f, 244f, 180f, 46f);

    /// <summary>B-right: the Deposit button of contract 5.1.</summary>
    public static PanelRect BRightDeposit => new PanelRect(1496f, 244f, 180f, 46f);

    /// <summary>B-right: the status line of contract 5.1.</summary>
    public static PanelRect BRightStatus => new PanelRect(1308f, 204f, 368f, 24f);

    /// <summary>B-below: the Organize button of contract 5.1.</summary>
    public static PanelRect BBelowOrganize => new PanelRect(1120f, 190f, 180f, 46f);

    /// <summary>B-below: the Deposit button of contract 5.1.</summary>
    public static PanelRect BBelowDeposit => new PanelRect(1308f, 190f, 180f, 46f);

    /// <summary>B-below: the status line of contract 5.1.</summary>
    public static PanelRect BBelowStatus => new PanelRect(1120f, 150f, 368f, 24f);

    /// <summary>The status text for 6 stations and 41 chests (contract 5.4, with its colour tags).</summary>
    public static string Status6Stations41Chests => "Base: <color=orange>6</color> stations, <color=orange>41</color> chests, <color=orange>0%</color> full";

    /// <summary>The status text for 6 stations and 0 chests.</summary>
    public static string Status6Stations0Chests => "Base: <color=orange>6</color> stations, <color=orange>0</color> chests, <color=orange>0%</color> full";

    /// <summary>The status text for 6 stations and 1 chest.</summary>
    public static string Status6Stations1Chest => "Base: <color=orange>6</color> stations, <color=orange>1</color> chest, <color=orange>0%</color> full";

    /// <summary>The status text for 1 station and 0 chests.</summary>
    public static string Status1Station0Chests => "Base: <color=orange>1</color> station, <color=orange>0</color> chests, <color=orange>0%</color> full";

    /// <summary>The status text for 1 station and 1 chest.</summary>
    public static string Status1Station1Chest => "Base: <color=orange>1</color> station, <color=orange>1</color> chest, <color=orange>0%</color> full";

    /// <summary>The status text for 1 station and 2 chests.</summary>
    public static string Status1Station2Chests => "Base: <color=orange>1</color> station, <color=orange>2</color> chests, <color=orange>0%</color> full";

    /// <summary>The status text for 2 stations and 1 chest.</summary>
    public static string Status2Stations1Chest => "Base: <color=orange>2</color> stations, <color=orange>1</color> chest, <color=orange>0%</color> full";

    /// <summary>The status text for 2 stations and 5 chests.</summary>
    public static string Status2Stations5Chests => "Base: <color=orange>2</color> stations, <color=orange>5</color> chests, <color=orange>0%</color> full";

    /// <summary>The status text for 3 stations and 12 chests.</summary>
    public static string Status3Stations12Chests => "Base: <color=orange>3</color> stations, <color=orange>12</color> chests, <color=orange>0%</color> full";

    /// <summary>The status text for 1234567 stations and int.MaxValue chests, with no digit grouping (GW-U-539).</summary>
    public static string StatusLargeCounts => "Base: <color=orange>1234567</color> stations, <color=orange>2147483647</color> chests, <color=orange>0%</color> full";

    /// <summary>The tooltip key shown while an Organize or a Deposit runs.</summary>
    public const string InProgressKey = "station.actions.disabled-in-progress";

    /// <summary>The tooltip key shown when the base has no eligible chests.</summary>
    public const string NoChestsKey = "station.actions.disabled-no-chests";

    /// <summary>Builds an input from explicit parts. The defaults are the contract defaults: 6 stations, 41 chests, no holder, on.</summary>
    /// <param name="screenWidth">The screen width in pixels.</param>
    /// <param name="screenHeight">The screen height in pixels.</param>
    /// <param name="uiScale">The UI scale.</param>
    /// <param name="recipes">The recipe list rectangle.</param>
    /// <param name="requirements">The requirements rectangle.</param>
    /// <param name="craft">The craft button rectangle.</param>
    /// <param name="stations">The station count.</param>
    /// <param name="chests">The eligible chest count.</param>
    /// <param name="p">The fill percentage, 0 to 100 (S-E010-001, station-panel contract v2 section 10).</param>
    /// <param name="holder">The holder name, or null.</param>
    /// <param name="on">The StationButtons setting.</param>
    /// <returns>The input.</returns>
    public static StationPanelInput In(
        int screenWidth,
        int screenHeight,
        float uiScale,
        PanelRect recipes,
        PanelRect requirements,
        PanelRect craft,
        int stations = 6,
        int chests = 41,
        int p = 0,
        string? holder = null,
        bool on = true)
    {
        return new StationPanelInput(screenWidth, screenHeight, uiScale, recipes, requirements, craft, stations, chests, p, holder, on);
    }

    /// <summary>Fixture A through the In helper.</summary>
    /// <param name="screenWidth">The screen width in pixels.</param>
    /// <param name="screenHeight">The screen height in pixels.</param>
    /// <param name="uiScale">The UI scale.</param>
    /// <param name="stations">The station count.</param>
    /// <param name="chests">The eligible chest count.</param>
    /// <param name="holder">The holder name, or null.</param>
    /// <param name="on">The StationButtons setting.</param>
    /// <returns>The input.</returns>
    public static StationPanelInput AIn(int screenWidth, int screenHeight, float uiScale, int stations = 6, int chests = 41, string? holder = null, bool on = true)
    {
        return In(screenWidth, screenHeight, uiScale, ARecipes, AReqs, ACraft, stations, chests, 0, holder, on);
    }

    /// <summary>Fixture B through the In helper.</summary>
    /// <param name="screenWidth">The screen width in pixels.</param>
    /// <param name="screenHeight">The screen height in pixels.</param>
    /// <param name="uiScale">The UI scale.</param>
    /// <returns>The input, with the contract defaults.</returns>
    public static StationPanelInput BIn(int screenWidth, int screenHeight, float uiScale)
    {
        return In(screenWidth, screenHeight, uiScale, BRecipes, BReqs, BCraft);
    }

    /// <summary>The three expected rectangles of a named contract row: "A-right", "B-right" or "B-below".</summary>
    /// <param name="name">The row name.</param>
    /// <returns>The Organize, Deposit and status rectangles.</returns>
    public static (PanelRect Organize, PanelRect Deposit, PanelRect Status) Rects(string name)
    {
        switch (name)
        {
            case "A-right":
                return (ARightOrganize, ARightDeposit, ARightStatus);
            case "B-right":
                return (BRightOrganize, BRightDeposit, BRightStatus);
            case "B-below":
                return (BBelowOrganize, BBelowDeposit, BBelowStatus);
            default:
                throw new ArgumentOutOfRangeException(nameof(name), name, "Unknown rectangle set.");
        }
    }

    /// <summary>Asserts that a visible state places both buttons and the status line at the given rectangles, within the canvas tolerance of 0.01.</summary>
    /// <param name="state">The state to check.</param>
    /// <param name="placement">The expected placement.</param>
    /// <param name="organize">The expected Organize rectangle.</param>
    /// <param name="deposit">The expected Deposit rectangle.</param>
    /// <param name="status">The expected status rectangle.</param>
    public static void AssertPlaced(StationPanelState state, StationPanelPlacement placement, PanelRect organize, PanelRect deposit, PanelRect status)
    {
        Assert.True(state.Visible, "The state must be visible.");
        Assert.Equal(placement, state.Placement);
        Assert.Equal(organize, state.OrganizeButton);
        Assert.Equal(deposit, state.DepositButton);
        Assert.Equal(status, state.StatusLine);
    }

    /// <summary>Asserts a canvas size with the SP4 tolerance of 0.01 units.</summary>
    /// <param name="state">The state to check.</param>
    /// <param name="width">The expected canvas width.</param>
    /// <param name="height">The expected canvas height.</param>
    public static void AssertCanvas(StationPanelState state, float width, float height)
    {
        Assert.InRange(state.CanvasWidth, width - 0.01f, width + 0.01f);
        Assert.InRange(state.CanvasHeight, height - 0.01f, height + 0.01f);
    }

    /// <summary>Runs an action with the given culture as both the current culture and the current UI culture, then restores both.</summary>
    /// <param name="name">The culture name. An empty name means the invariant culture.</param>
    /// <param name="action">The action to run.</param>
    public static void WithCulture(string name, Action action)
    {
        CultureInfo culture = name.Length == 0 ? CultureInfo.InvariantCulture : CultureInfo.GetCultureInfo(name);
        CultureInfo previousCulture = CultureInfo.CurrentCulture;
        CultureInfo previousUi = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;
            action();
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
            CultureInfo.CurrentUICulture = previousUi;
        }
    }
}
