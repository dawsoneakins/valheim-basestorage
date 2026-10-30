namespace BaseStorage.Core.Ui;

/// <summary>Who shows the one line of an outcome (contract feedback-audit FG1, FG3).</summary>
public enum FeedbackLineOwner
{
    /// <summary>BaseStorage shows the one line, from the strings table.</summary>
    Mod = 0,

    /// <summary>The game shows its own line for this outcome; BaseStorage adds none.</summary>
    Vanilla = 1,

    /// <summary>There is no local player or world, so there is no HUD.</summary>
    NoHud = 2,

    /// <summary>The player's own next input ended the action, and that input has its own line.</summary>
    Superseded = 3,

    /// <summary>The same request is already in flight, and its line is the one line.</summary>
    Coalesced = 4,

    /// <summary>The chest the action targeted was destroyed or unloaded.</summary>
    TargetGone = 5,
}
