namespace BaseStorage.Core.Tests.Ui;

/// <summary>S-FOUND-001 AC-4: English plural selection and multi-count variants (contract 2.2 S7, S8).</summary>
public sealed class PluralTests
{
    // GW-U-054
    [Fact]
    public void PluralConstantsAreOneAndOther()
    {
        Assert.Equal("one", UiContract.PluralOne());
        Assert.Equal("other", UiContract.PluralOther());
    }

    // GW-U-054
    [Fact]
    public void FormReturnsOneOnlyForOne()
    {
        Assert.Equal("one", UiContract.PluralForm(1));
    }

    // GW-U-054
    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    [InlineData(int.MaxValue)]
    public void FormReturnsOtherForEveryOtherCount(int count)
    {
        Assert.Equal("other", UiContract.PluralForm(count));
    }

    // GW-U-054
    [Theory]
    [MemberData(nameof(UiFixtures.PluralRows), MemberType = typeof(UiFixtures))]
    public void FormatPluralSelectsTheContractVariant(string key, int count, string expected)
    {
        Assert.Equal(expected, UiContract.FormatPlural(key, count, count));
    }

    // GW-U-054
    [Theory]
    [MemberData(nameof(UiFixtures.StatusLineRows), MemberType = typeof(UiFixtures))]
    public void StatusLineBuiltKeySelectsTwoCountVariant(int stations, int chests, string expected)
    {
        string key = "station.actions.status-line." + UiContract.PluralForm(stations) + "." + UiContract.PluralForm(chests);

        Assert.Equal(expected, UiContract.Format(key, stations, chests));
    }

    // GW-U-054
    [Theory]
    [MemberData(nameof(UiFixtures.MoveClauseRows), MemberType = typeof(UiFixtures))]
    public void MoveClauseBuiltKeySelectsTwoCountVariant(int stacks, int chests, string expected)
    {
        string key = "organize.preview.body.move." + UiContract.PluralForm(stacks) + "." + UiContract.PluralForm(chests);

        Assert.Equal(expected, UiContract.Format(key, stacks, chests));
    }

    // GW-U-054
    [Fact]
    public void PreviewExampleGivesTheFourClauseTexts()
    {
        object?[] args = UiFixtures.PreviewArgs();
        string move = UiContract.Format("organize.preview.body.move." + UiContract.PluralForm(37) + "." + UiContract.PluralForm(5), args);
        string claim = UiContract.FormatPlural("organize.preview.body.claim", 2, args);
        string noRoom = UiContract.FormatPlural("organize.preview.body.no-room", 4, args);
        string skipped = UiContract.FormatPlural("organize.preview.body.skipped", 1, args);

        Assert.Equal(UiFixtures.PreviewClauseTexts[0], move);
        Assert.Equal(UiFixtures.PreviewClauseTexts[1], claim);
        Assert.Equal(UiFixtures.PreviewClauseTexts[2], noRoom);
        Assert.Equal(UiFixtures.PreviewClauseTexts[3], skipped);
    }

    // GW-U-054
    [Fact]
    public void FormatPluralDoesNotInsertTheCount()
    {
        Assert.Equal("[In base: {0} stations]", UiContract.FormatPlural("hover.chest.in-base", 6));
    }
}
