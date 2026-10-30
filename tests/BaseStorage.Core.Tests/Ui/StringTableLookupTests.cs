namespace BaseStorage.Core.Tests.Ui;

/// <summary>S-FOUND-001 AC-4: lookup, ordinal keys, missing-key fallback and null-key rejection (contract 2.2 S1, S2, S3, U4).</summary>
public sealed class StringTableLookupTests
{
    // GW-U-051
    [Fact]
    public void GetReturnsTheRawTemplateWithPlaceholdersUnsubstituted()
    {
        Assert.Equal("Not in a base", UiContract.Get("message.base.not-in-base"));
        Assert.Equal("Organize in progress by {0}", UiContract.Get("message.base.in-progress"));
    }

    // GW-U-051
    [Fact]
    public void LookupIsCaseSensitiveAndOrdinal()
    {
        Assert.Equal("Message.base.not-in-base", UiContract.Get("Message.base.not-in-base"));
        Assert.False(UiContract.Contains("Message.base.not-in-base"));
    }

    // GW-U-051
    [Fact]
    public void EmptyKeyIsAValidMissingKeyThatReturnsEmpty()
    {
        Assert.Equal(string.Empty, UiContract.Get(string.Empty));
    }

    // GW-U-051
    [Fact]
    public void PluralBaseIsNotContainedButItsOtherVariantIs()
    {
        Assert.True(UiContract.Contains("hover.chest.in-base.other"));
        Assert.False(UiContract.Contains("hover.chest.in-base"));
    }

    // GW-U-055
    [Fact]
    public void MissingKeyFallsBackToTheKeyName()
    {
        Assert.Equal("no.such.key", UiContract.Get("no.such.key"));
        Assert.Equal("no.such.key", UiContract.Format("no.such.key", 1, 2));
    }

    // GW-U-055
    [Fact]
    public void MissingPluralKeyReturnsTheSelectedFullKeyName()
    {
        Assert.Equal("no.such.key.one", UiContract.FormatPlural("no.such.key", 1));
        Assert.Equal("no.such.key.other", UiContract.FormatPlural("no.such.key", 2));
    }

    // GW-U-055
    [Fact]
    public void NoCrossVariantFallbackOccurs()
    {
        Assert.Equal("hover.chest.not-in-base.one", UiContract.FormatPlural("hover.chest.not-in-base", 1));
        Assert.Equal("station.actions.status-line.one", UiContract.Format("station.actions.status-line.one"));
    }

    // GW-U-056
    [Fact]
    public void GetWithNullKeyThrowsArgumentNullExceptionNamedKey()
    {
        UiContract.English();

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => UiContract.Get(null));

        Assert.Equal("key", ex.ParamName);
    }

    // GW-U-056
    [Fact]
    public void ContainsWithNullKeyThrowsArgumentNullExceptionNamedKey()
    {
        UiContract.English();

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => UiContract.Contains(null));

        Assert.Equal("key", ex.ParamName);
    }

    // GW-U-056
    [Fact]
    public void FormatWithNullKeyThrowsArgumentNullExceptionNamedKey()
    {
        UiContract.English();

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => UiContract.Format(null));

        Assert.Equal("key", ex.ParamName);
    }

    // GW-U-056
    [Fact]
    public void FormatPluralWithNullKeyThrowsArgumentNullExceptionNamedKey()
    {
        UiContract.English();

        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => UiContract.FormatPlural(null, 1));

        Assert.Equal("key", ex.ParamName);
    }
}
