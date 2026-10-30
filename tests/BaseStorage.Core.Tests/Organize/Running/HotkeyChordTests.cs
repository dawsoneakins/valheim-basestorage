namespace BaseStorage.Core.Tests.Organize.Running;

using BaseStorage.Core.Organize.Run;

/// <summary>GW-U-1374 (AC-3, contract 4.4): the shortcut text parses into key names, and every unbound form is unbound.</summary>
public sealed class HotkeyChordTests
{
    // GW-U-1374
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("None")]
    [InlineData("+LeftShift")]
    [InlineData(" + ")]
    public void UnboundFormsAreUnbound(string? text)
    {
        HotkeyChord chord = HotkeyChord.Parse(text);

        Assert.False(chord.IsBound);
        Assert.Equal(string.Empty, chord.MainKey);
        Assert.Empty(chord.Modifiers);
        Assert.False(HotkeyChord.Unbound.IsBound);
    }

    // GW-U-1374
    [Theory]
    [InlineData("G", "G", new string[0])]
    [InlineData("G+LeftShift", "G", new[] { "LeftShift" })]
    [InlineData("G+LeftShift+LeftControl", "G", new[] { "LeftShift", "LeftControl" })]
    [InlineData(" G + LeftShift ", "G", new[] { "LeftShift" })]
    [InlineData("G++LeftAlt", "G", new[] { "LeftAlt" })]
    [InlineData("G+None", "G", new string[0])]
    [InlineData("Keypad7", "Keypad7", new string[0])]
    public void BoundFormsSplitIntoNames(string text, string main, string[] modifiers)
    {
        HotkeyChord chord = HotkeyChord.Parse(text);

        Assert.True(chord.IsBound);
        Assert.Equal(main, chord.MainKey);
        Assert.Equal(modifiers, chord.Modifiers);
    }
}
