namespace BaseStorage.Core.Availability;

/// <summary>How a requirement treats the quality of an item stack (contract section 9.1, rule C5).</summary>
public enum QualityMatch
{
    /// <summary>Every quality counts (the vanilla crafting, building and upgrade rule).</summary>
    Any = 0,

    /// <summary>Only stacks whose quality equals the requirement's quality count.</summary>
    Exact = 1,

    /// <summary>Only stacks whose quality is at least the requirement's quality count.</summary>
    AtLeast = 2,
}
