namespace BaseStorage.PatchTargets.Tests.Fixtures.Game;

using System.Collections.Generic;

/// <summary>A fixture whose field has a constructed generic type, like Player.m_customData (contract 6.3, rule PT1).</summary>
public sealed class ProfileHolder
{
    /// <summary>The fixture custom data field.</summary>
    public Dictionary<string, string> m_customData = new();
}
