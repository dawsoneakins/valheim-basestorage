namespace BaseStorage.Core.Tests.Bases;

using System;
using System.Linq;
using BaseStorage.Core.Bases.Inspection;

/// <summary>AC-1 (ADR-GWIN-032, contract H2): Append adds exactly one line, and adds nothing when vanilla shows no text.</summary>
public sealed class HoverAppendTests
{
    private const string Vanilla = "Chest\n[<color=yellow><b>E</b></color>] Open";

    // GW-U-536 (AC-1): the literal result.
    [Fact]
    public void AppendAddsTheLineAfterTheVanillaTextWithOneBreak()
    {
        Assert.Equal(
            "Chest\n[<color=yellow><b>E</b></color>] Open\n[Not in a base]",
            HoverInspector.Append(Vanilla, "[Not in a base]"));
    }

    // GW-U-536 (AC-1): every formatter output adds exactly one break and ends with the line.
    [Theory]
    [InlineData("a chest in one station's base", "[In base: 1 station]")]
    [InlineData("a chest in two stations' base", "[In base: 2 stations]")]
    [InlineData("a chest in six stations' base", "[In base: 6 stations]")]
    [InlineData("a chest in no base", "[Not in a base]")]
    [InlineData("a chest with a category", "[In base: 2 stations] [Ores]")]
    [InlineData("a station with no chests", "[Base: 0 chests]")]
    [InlineData("a station with one chest", "[Base: 1 chest]")]
    [InlineData("a station with two chests", "[Base: 2 chests]")]
    public void AppendAddsExactlyOneBreakAndEndsWithTheLine(string row, string line)
    {
        string? result = HoverInspector.Append(Vanilla, line);

        Assert.NotNull(result);
        Assert.Equal(Vanilla.Count(c => c == '\n') + 1, result!.Count(c => c == '\n'));
        Assert.True(result.EndsWith("\n" + line, StringComparison.Ordinal), $"[{row}] the result does not end with the line.");
    }

    // GW-U-536 (AC-1): an empty line adds nothing, and the same instance comes back.
    [Fact]
    public void AppendOfAnEmptyLineReturnsTheTextItself()
    {
        string text = Vanilla;

        Assert.Same(text, HoverInspector.Append(text, string.Empty));
    }

    // GW-U-536 (AC-1): vanilla shows no text, so the mod shows nothing either.
    [Fact]
    public void AppendToNullTextReturnsNull()
    {
        Assert.Null(HoverInspector.Append(null, "[Not in a base]"));
    }

    // GW-U-536 (AC-1): vanilla shows an empty text, so the mod adds nothing.
    [Fact]
    public void AppendToEmptyTextReturnsTheEmptyText()
    {
        Assert.Equal(string.Empty, HoverInspector.Append(string.Empty, "[Not in a base]"));
    }

    // GW-U-536 (AC-1): a null line is an argument error, reported by parameter name.
    [Fact]
    public void AppendWithANullLineThrowsForLine()
    {
        string text = Vanilla;

        ArgumentNullException error = Assert.Throws<ArgumentNullException>(() => HoverInspector.Append(text, null!));

        Assert.Equal("line", error.ParamName);
    }
}
