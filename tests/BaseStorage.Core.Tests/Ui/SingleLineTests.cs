namespace BaseStorage.Core.Tests.Ui;

/// <summary>S-FOUND-001 AC-2: SingleLine.Of replaces each run of line-break characters with one space (contract 4).</summary>
public sealed class SingleLineTests
{
    // GW-U-057
    [Theory]
    [MemberData(nameof(UiFixtures.SingleLineRows), MemberType = typeof(UiFixtures))]
    public void EachRunOfBreakCharactersBecomesOneSpace(string input, string expected)
    {
        Assert.Equal(expected, UiContract.SingleLineOf(input));
    }

    // GW-U-057
    [Fact]
    public void NullTextThrowsArgumentNullExceptionNamedText()
    {
        UiContract.Find("SingleLine");

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => UiContract.SingleLineOf(null));

        Assert.Equal("text", ex.ParamName);
    }
}
