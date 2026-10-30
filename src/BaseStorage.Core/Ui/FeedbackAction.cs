namespace BaseStorage.Core.Ui;

/// <summary>The user action an outcome belongs to (contract feedback-audit 2.1).</summary>
public enum FeedbackAction
{
    /// <summary>Crafting at a station with materials from base chests.</summary>
    Craft = 0,

    /// <summary>Placing a piece with materials from base chests.</summary>
    Build = 1,

    /// <summary>Changing the category of a chest.</summary>
    CategoryChange = 2,

    /// <summary>Organizing the base.</summary>
    Organize = 3,

    /// <summary>Putting the inventory away into the base.</summary>
    Deposit = 4,

    /// <summary>Toggling a favourite slot.</summary>
    Favourite = 5,
}
