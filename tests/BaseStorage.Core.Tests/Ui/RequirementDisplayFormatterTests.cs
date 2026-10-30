namespace BaseStorage.Core.Tests.Ui;

using System;
using System.Globalization;
using BaseStorage.Core.Ui;

/// <summary>
/// GW-U-1500 to GW-U-1508 (AC-1 to AC-3; contract 3.1 to 3.3, rows F1-F22): the Core requirement formatter and the equality of
/// <see cref="RequirementDisplay"/>. Expected values are the literals of the contract tables. Core only; no Plugin code is loaded.
/// </summary>
public sealed class RequirementDisplayFormatterTests
{
    private const string Marker = "vanilla-not-used";

    // GW-U-1500 (AC-1, 3.2 step 4): the combined form for F1-F6, F11, F12 and F13; the vanilla text is not part of the result.
    [Theory]
    [InlineData(12, 40, 20, "12 (+40) / 20")]
    [InlineData(5, 10, 20, "5 (+10) / 20")]
    [InlineData(8, 12, 20, "8 (+12) / 20")]
    [InlineData(0, 20, 20, "0 (+20) / 20")]
    [InlineData(0, 19, 20, "0 (+19) / 20")]
    [InlineData(25, 3, 20, "25 (+3) / 20")]
    [InlineData(1234, 5678, 10000, "1234 (+5678) / 10000")]
    [InlineData(int.MaxValue, int.MaxValue, int.MaxValue, "2147483647 (+2147483647) / 2147483647")]
    [InlineData(3, 2, 0, "3 (+2) / 0")]
    public void TheCombinedFormShowsTheChestAmountAndIgnoresTheVanillaText(int have, int chest, int need, string expected)
    {
        RequirementDisplay result = RequirementDisplayFormatter.Format(have, chest, need, Marker);

        Assert.Equal(expected, result.Text);
        Assert.True(result.ShowsChests);
    }

    // GW-U-1501 (AC-1, 3.2 step 4 and 3.3 note): F1 and F11 are the same under de-DE, fr-FR and ar-SA; the culture is restored.
    [Theory]
    [InlineData("de-DE", 12, 40, 20, "12 (+40) / 20")]
    [InlineData("de-DE", 1234, 5678, 10000, "1234 (+5678) / 10000")]
    [InlineData("fr-FR", 12, 40, 20, "12 (+40) / 20")]
    [InlineData("fr-FR", 1234, 5678, 10000, "1234 (+5678) / 10000")]
    [InlineData("ar-SA", 12, 40, 20, "12 (+40) / 20")]
    [InlineData("ar-SA", 1234, 5678, 10000, "1234 (+5678) / 10000")]
    public void TheTextIsTheSameUnderAnyCurrentCulture(string culture, int have, int chest, int need, string expected)
    {
        CultureInfo previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo(culture);

            Assert.Equal(expected, RequirementDisplayFormatter.Format(have, chest, need, Marker).Text);
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    // GW-U-1502 (AC-2, 3.2 step 3): F7, F8, F9, F10 and F14 give the vanilla text as the same instance, and no chest is shown.
    [Theory]
    [InlineData(12, 0, 20, "20", true)]
    [InlineData(20, 0, 20, "20", false)]
    [InlineData(30, 0, 20, "20", false)]
    [InlineData(0, 0, 1, "1", true)]
    [InlineData(7, 0, 5, "", false)]
    public void WithoutAChestTheVanillaTextIsReturnedAsTheSameInstance(int have, int chest, int need, string vanilla, bool isShort)
    {
        RequirementDisplay result = RequirementDisplayFormatter.Format(have, chest, need, vanilla);

        Assert.Same(vanilla, result.Text);
        Assert.False(result.ShowsChests);
        Assert.Equal(isShort, result.IsShort);
    }

    // GW-U-1503 (AC-3): short combined amounts are red, checked through Format (F2, F5, F7, F10, F11).
    [Theory]
    [InlineData(5, 10, 20, "20")]
    [InlineData(0, 19, 20, "20")]
    [InlineData(12, 0, 20, "20")]
    [InlineData(0, 0, 1, "1")]
    [InlineData(1234, 5678, 10000, "10000")]
    public void ShortThroughFormatIsRed(int have, int chest, int need, string vanilla)
    {
        Assert.True(RequirementDisplayFormatter.Format(have, chest, need, vanilla).IsShort);
    }

    // GW-U-1503 (AC-3): the same five short rows through IsShort alone.
    [Theory]
    [InlineData(5, 10, 20)]
    [InlineData(0, 19, 20)]
    [InlineData(12, 0, 20)]
    [InlineData(0, 0, 1)]
    [InlineData(1234, 5678, 10000)]
    public void ShortThroughIsShortAloneIsRed(int have, int chest, int need)
    {
        Assert.True(RequirementDisplayFormatter.IsShort(have, chest, need));
    }

    // GW-U-1504 (AC-3): exact amounts are not red, checked through Format (F3, F4, F8).
    [Theory]
    [InlineData(8, 12, 20, "20")]
    [InlineData(0, 20, 20, "20")]
    [InlineData(20, 0, 20, "20")]
    public void ExactThroughFormatIsNotRed(int have, int chest, int need, string vanilla)
    {
        Assert.False(RequirementDisplayFormatter.Format(have, chest, need, vanilla).IsShort);
    }

    // GW-U-1504 (AC-3): the same three exact rows through IsShort alone.
    [Theory]
    [InlineData(8, 12, 20)]
    [InlineData(0, 20, 20)]
    [InlineData(20, 0, 20)]
    public void ExactThroughIsShortAloneIsNotRed(int have, int chest, int need)
    {
        Assert.False(RequirementDisplayFormatter.IsShort(have, chest, need));
    }

    // GW-U-1505 (AC-3): surplus is not red, checked through Format (F1, F6, F9); F6 still shows the chest.
    [Theory]
    [InlineData(12, 40, 20, "20")]
    [InlineData(25, 3, 20, "20")]
    [InlineData(30, 0, 20, "20")]
    public void SurplusThroughFormatIsNotRed(int have, int chest, int need, string vanilla)
    {
        Assert.False(RequirementDisplayFormatter.Format(have, chest, need, vanilla).IsShort);
    }

    // GW-U-1505 (AC-3): the same three surplus rows through IsShort alone.
    [Theory]
    [InlineData(12, 40, 20)]
    [InlineData(25, 3, 20)]
    [InlineData(30, 0, 20)]
    public void SurplusThroughIsShortAloneIsNotRed(int have, int chest, int need)
    {
        Assert.False(RequirementDisplayFormatter.IsShort(have, chest, need));
    }

    // GW-U-1506 (AC-3, 3.2 step 2): IsShort alone equals the IsShort column of F1-F14 (F12 and F13 are also checked in their own facts).
    [Theory]
    [InlineData(12, 40, 20, false)]
    [InlineData(5, 10, 20, true)]
    [InlineData(8, 12, 20, false)]
    [InlineData(0, 20, 20, false)]
    [InlineData(0, 19, 20, true)]
    [InlineData(25, 3, 20, false)]
    [InlineData(12, 0, 20, true)]
    [InlineData(20, 0, 20, false)]
    [InlineData(30, 0, 20, false)]
    [InlineData(0, 0, 1, true)]
    [InlineData(1234, 5678, 10000, true)]
    [InlineData(int.MaxValue, int.MaxValue, int.MaxValue, false)]
    [InlineData(3, 2, 0, false)]
    [InlineData(7, 0, 5, false)]
    public void IsShortAloneMatchesTheContractTable(int have, int chest, int need, bool expected)
    {
        Assert.Equal(expected, RequirementDisplayFormatter.IsShort(have, chest, need));
    }

    // GW-U-1506 (AC-3, F12): int.MaxValue three times does not overflow and is not short.
    [Fact]
    public void TheMaximumAmountsDoNotOverflowIntoAShortage()
    {
        Assert.False(RequirementDisplayFormatter.IsShort(int.MaxValue, int.MaxValue, int.MaxValue));
        Assert.False(RequirementDisplayFormatter.Format(int.MaxValue, int.MaxValue, int.MaxValue, "2147483647").IsShort);
    }

    // GW-U-1506 (AC-3, F13): a need of 0 is accepted and is not short.
    [Fact]
    public void ANeedOfZeroIsAcceptedAndIsNotShort()
    {
        Assert.False(RequirementDisplayFormatter.IsShort(3, 2, 0));
        Assert.False(RequirementDisplayFormatter.Format(3, 2, 0, "0").IsShort);
    }

    // GW-U-1507 (AC-1, 3.2 step 1, R8): F15 negative have is reported by ParamName.
    [Fact]
    public void ANegativeHaveIsRejectedByName()
    {
        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() => { RequirementDisplayFormatter.Format(-1, 0, 20, "20"); });

        Assert.Equal("have", ex.ParamName);
    }

    // GW-U-1507 (AC-1, 3.2 step 1): F16 negative chest is reported by ParamName.
    [Fact]
    public void ANegativeChestIsRejectedByName()
    {
        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() => { RequirementDisplayFormatter.Format(0, -1, 20, "20"); });

        Assert.Equal("chest", ex.ParamName);
    }

    // GW-U-1507 (AC-1, 3.2 step 1): F17 negative need is reported by ParamName.
    [Fact]
    public void ANegativeNeedIsRejectedByName()
    {
        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() => { RequirementDisplayFormatter.Format(0, 0, -1, "-1"); });

        Assert.Equal("need", ex.ParamName);
    }

    // GW-U-1507 (AC-1, 3.2 step 1): F18 a null vanilla text is reported by ParamName.
    [Fact]
    public void ANullVanillaTextIsRejectedByName()
    {
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => { RequirementDisplayFormatter.Format(0, 0, 20, null!); });

        Assert.Equal("vanillaText", ex.ParamName);
    }

    // GW-U-1507 (AC-1, 3.2 step 1, R8): F19 with every argument invalid reports the first in parameter order, have.
    [Fact]
    public void WhenEveryArgumentIsInvalidTheFirstInParameterOrderIsReported()
    {
        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() => { RequirementDisplayFormatter.Format(-1, -1, -1, null!); });

        Assert.Equal("have", ex.ParamName);
    }

    // GW-U-1507 (AC-1, 3.2 step 2): F20 IsShort rejects a negative need by ParamName.
    [Fact]
    public void IsShortRejectsANegativeNeedByName()
    {
        ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(() => { RequirementDisplayFormatter.IsShort(0, 0, -1); });

        Assert.Equal("need", ex.ParamName);
    }

    // GW-U-1507 (AC-1, 3.1, R8): F21 a null text is reported by ParamName.
    [Fact]
    public void ANullTextInTheConstructorIsRejectedByName()
    {
        ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => { _ = new RequirementDisplay(null!, false, false); });

        Assert.Equal("text", ex.ParamName);
    }

    // GW-U-1508 (AC-1, 3.1, R5, R6, F22): equal values are equal with equal hashes; a different ShowsChests is not equal.
    [Fact]
    public void FormattedValuesCompareByValueAndHash()
    {
        RequirementDisplay plain = RequirementDisplayFormatter.Format(12, 0, 20, "20");
        RequirementDisplay expected = new RequirementDisplay("20", true, false);
        RequirementDisplay other = new RequirementDisplay("20", true, true);

        Assert.True(expected == plain);
        Assert.Equal(expected.GetHashCode(), plain.GetHashCode());
        Assert.False(other == plain);
    }

    // GW-U-1508 (AC-1, 3.1, R5): Text compares ordinally, so "a" and "A" differ.
    [Fact]
    public void TheTextComparesOrdinally()
    {
        RequirementDisplay lower = new RequirementDisplay("a", false, false);
        RequirementDisplay upper = new RequirementDisplay("A", false, false);

        Assert.False(lower == upper);
        Assert.True(lower != upper);
    }

    // GW-U-1508 (AC-1, 3.1, R6): default equals another default, differs from an empty text, and never throws.
    [Fact]
    public void TheDefaultValueEqualsItselfAndDiffersFromAnEmptyText()
    {
        RequirementDisplay empty = default;
        RequirementDisplay blank = new RequirementDisplay(string.Empty, false, false);

        Assert.True(empty.Equals(default(RequirementDisplay)));
        Assert.True(empty == default(RequirementDisplay));
        Assert.False(empty.Equals(blank));
        Assert.False(empty == blank);
    }

    // GW-U-1508 (AC-1, 3.1, R5): the operators and Equals(object) agree.
    [Fact]
    public void TheOperatorsAndEqualsAgree()
    {
        RequirementDisplay first = new RequirementDisplay("20", true, false);
        RequirementDisplay same = new RequirementDisplay("20", true, false);
        RequirementDisplay other = new RequirementDisplay("20", true, true);

        Assert.True(first == same);
        Assert.False(first != same);
        Assert.True(first != other);
        Assert.False(first == other);
        Assert.Equal(first == same, first.Equals(same));
        Assert.Equal(first == other, first.Equals(other));
    }

    // GW-U-1508 (AC-1, 3.1, R5): Equals(object) accepts a boxed equal value and rejects null and another type.
    [Fact]
    public void EqualsObjectAcceptsABoxedValueAndRejectsNullAndOtherTypes()
    {
        RequirementDisplay first = new RequirementDisplay("20", true, false);
        object boxed = new RequirementDisplay("20", true, false);

        Assert.True(first.Equals(boxed));
        Assert.False(first.Equals((object?)null));
        Assert.False(first.Equals((object)"20"));
    }
}
