namespace BaseStorage.Core.Bases.Resolution;

/// <summary>What a base lookup starts from (ADR-GWIN-024): a crafting station, or a position in the world.</summary>
public enum BaseStartKind
{
    /// <summary>The lookup starts from a crafting station, which must be an eligible station to find a base.</summary>
    Station = 1,

    /// <summary>The lookup starts from a world position, and uses the lowest-id eligible station whose build range contains it.</summary>
    Position = 2,
}
