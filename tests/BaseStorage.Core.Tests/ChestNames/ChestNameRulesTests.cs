namespace BaseStorage.Core.Tests.ChestNames;

using BaseStorage.Core.ChestNames;

/// <summary>Chest names (S-QL-001), which come from other players and reach their screens.</summary>
public sealed class ChestNameRulesTests
{
    [Theory]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData("   ", "")]
    [InlineData("  Ores  ", "Ores")]
    [InlineData("a\nb", "a b")]
    [InlineData("a\rb\tc", "a b c")]
    [InlineData("a\u2028b", "a b")]
    public void NamesAreTrimmedAndKeptToOneLine(string? raw, string expected)
    {
        Assert.Equal(expected, ChestNameRules.Clean(raw));
    }

    [Theory]
    [InlineData("<color=red>Loot</color>", "color=redLoot/color")]
    [InlineData("<size=500>", "size=500")]
    [InlineData("<<>>", "")]
    public void RichTextCannotReachAnotherPlayersScreen(string raw, string expected)
    {
        Assert.Equal(expected, ChestNameRules.Clean(raw));
    }

    [Fact]
    public void ALongNameIsCapped()
    {
        Assert.Equal(ChestNameRules.MaxLength, ChestNameRules.Clean(new string('a', 500)).Length);
    }

    [Fact]
    public void TheCapNeverSplitsASurrogatePair()
    {
        string name = new string('a', ChestNameRules.MaxLength - 1) + "\U0001F600";

        string clean = ChestNameRules.Clean(name);

        Assert.Equal(ChestNameRules.MaxLength - 1, clean.Length);
        Assert.False(char.IsHighSurrogate(clean[clean.Length - 1]));
    }
}
