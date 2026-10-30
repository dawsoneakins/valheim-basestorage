namespace BaseStorage.Core.Tests.Ui;

using System.Globalization;

/// <summary>S-FOUND-001 AC-4: placeholder formatting, argument handling and invariant culture (contract 2.2 S4-S6, U2).</summary>
public sealed class StringTableFormatTests
{
    // GW-U-052
    [Fact]
    public void FormatSubstitutesASinglePlaceholder()
    {
        Assert.Equal(UiFixtures.InProgressErikText, UiContract.Format("message.base.in-progress", "Erik"));
    }

    // GW-U-052
    [Fact]
    public void FormatSubstitutesTwoPlaceholders()
    {
        Assert.Equal(
            UiFixtures.VersionMismatchText,
            UiContract.Format("network.version.mismatch", "0.1.0", "0.2.0"));
    }

    // GW-U-052
    [Fact]
    public void FormatSubstitutesFourPlaceholdersInTheSummary()
    {
        Assert.Equal(UiFixtures.OrganizeSummaryText, UiContract.Format("message.organize.summary", 37, 2, "in use", 4));
    }

    // GW-U-052
    [Fact]
    public void FormatWithNoPlaceholdersAndNoArgsReturnsTheText()
    {
        Assert.Equal(UiFixtures.MaterialsMovedText, UiContract.Format("message.craft.materials-moved"));
    }

    // GW-U-052
    [Fact]
    public void FormatIgnoresExtraArguments()
    {
        Assert.Equal(UiFixtures.MaterialsMovedText, UiContract.Format("message.craft.materials-moved", "x", 7));
    }

    // GW-U-052
    [Fact]
    public void FormatWithTooFewArgumentsReturnsTheTemplateUnformatted()
    {
        Assert.Equal(UiFixtures.VersionMismatchTemplate, UiContract.Format("network.version.mismatch", "0.1.0"));
    }

    // GW-U-052
    [Fact]
    public void FormatWithANullArgsArrayReturnsTheTemplateUnformatted()
    {
        Assert.Equal(UiFixtures.InProgressTemplate, UiContract.Format("message.base.in-progress", (object?[]?)null));
    }

    // GW-U-052
    [Fact]
    public void FormatWithANullElementFormatsItAsEmpty()
    {
        Assert.Equal("Organize in progress by ", UiContract.Format("message.base.in-progress", new object?[] { null }));
    }

    // GW-U-053
    [Theory]
    [InlineData("de-DE")]
    [InlineData("tr-TR")]
    [InlineData("en-US")]
    public void FormattingIsInvariantAcrossCultures(string cultureName)
    {
        CultureInfo savedCurrent = CultureInfo.CurrentCulture;
        CultureInfo savedUi = CultureInfo.CurrentUICulture;
        try
        {
            var culture = new CultureInfo(cultureName);
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;

            Assert.Equal(UiFixtures.StatusLineFillText, UiContract.Format("station.actions.status-line-fill", 1.5));
            Assert.Equal(
                UiFixtures.VersionMismatchText,
                UiContract.Format("network.version.mismatch", "0.1.0", "0.2.0"));
        }
        finally
        {
            CultureInfo.CurrentCulture = savedCurrent;
            CultureInfo.CurrentUICulture = savedUi;
        }
    }
}
